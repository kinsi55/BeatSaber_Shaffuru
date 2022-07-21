using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Shaffuru.Util;
using Unity.Jobs;
using Zenject;
using static Shaffuru.AppLogic.MapPool;

namespace Shaffuru.AppLogic {
	class RequestManager : IInitializable, IDisposable {
		readonly SongQueue songQueue;

		readonly MapPool mapPool;
		readonly IChatMessageSource chatSource;

		public RequestManager(SongQueue songQueue, MapPool mapPool, IChatMessageSource chatSource) {
			this.songQueue = songQueue;
			this.mapPool = mapPool;

			this.chatSource = chatSource;
		}

		public void Initialize() {
			chatSource.OnTextMessageReceived += Twitch_OnTextMessageReceived;
		}

		public void Dispose() {
			if(chatSource == null)
				return;

			chatSource.Dispose();
			chatSource.OnTextMessageReceived -= Twitch_OnTextMessageReceived;
		}

		void Msg(string message, string sender, object channel) => chatSource.SendChatMessage($"! @{sender} {message}", channel);

		public delegate void ChatHandler(string sender, string message, string[] splitMessage, Action<string> sendResponse);
		ChatHandler chatHandler;

		private void Twitch_OnTextMessageReceived(string sender, string message, object channel) {
			if(!Config.Instance.chat_request_enabled || (!message.StartsWith("!chaos", StringComparison.OrdinalIgnoreCase) && !message.StartsWith("!sr", StringComparison.OrdinalIgnoreCase)))
				return;

			if(chatHandler == null) {
				Msg(sender, "Shaffuru is not initialized", channel);
				return;
			}

			// This should be more than enough characters for any request. !chaos FFFFFF 120:00 expertplus is 31 chars
			if(message.Length > 32)
				return;

			// When passing null, Split assumes you want to split by whitespace
			var split = message.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

			if(split.Length < 2)
				return;

			if(split[1].StartsWith("!bsr", StringComparison.OrdinalIgnoreCase)) {
				Msg("Pepega", sender, channel);
				return;
			}

			chatHandler(sender, message, split, (resp) => Msg(resp, sender, channel));
		}

		public void SetHandler(ChatHandler handler = null) {
			chatHandler = handler;
		}

		public void SetDefaultHandler() {
			SetHandler(MainHandler);
		}

		public class RequestResultSong {
			public bool requiredDownload = false;
			public string theKeyAndPossiblyMapName;
			public ShaffuruSong shaffuruSong;
		}


		public static Regex diffTimePattern = new Regex(@"(?<diff>Easy|Normal|Hard|Expert|ExpertPlus)?\s*((?<timeM>[0-9]{1,2}):(?<timeS>[0-5]?[0-9])|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public async Task<RequestResultSong> ProcessRequest(string sender, string message, string[] split, Action<string> sendResponse, bool respondForDownload = true) {
			if(mapPool.filteredLevels == null || SongDetailsUtil.instance == null) {
				sendResponse("Shaffuru is not initialized");
				return null;
			}

			if(songQueue.IsFull()) {
				sendResponse("The queue is full");
				return null;
			}

			return await Task.Run(() => {
				var song = SongDetailsCache.Structs.Song.none;

				var key = split[1].ToLowerInvariant();
				if(key.Length > 10 || !SongDetailsUtil.instance.songs.FindByMapId(key, out song)) {
					sendResponse("Unknown map ID");
					return null;
				}

				var hash = song.hash;
				var levelId = $"custom_level_{hash}";
				var mapNeedsDownload = false;

				if(!mapPool.LevelHashRequestable(hash)) {
					/*
					 * When filtering by a Playlist, if a map is downloaded, but it is not in the requestable pool We'll
					 * just assume its not in the playlist - Even if it could possibly be just not downloaded
					 */
					if(SongCore.Collections.hashForLevelID(levelId) != string.Empty || mapPool.isFilteredByPlaylist) {
						sendResponse("The map does not match the configured filters");
						return null;
					} else if(!Config.Instance.request_allowDownloading) {
						sendResponse("The map is not downloaded");
						return null;
					} else if(SongDownloaderJob.downloadingMaps.Contains(song.mapId)) {
						sendResponse("The map is currently being downloaded");
						return null;
					} else {
						mapNeedsDownload = true;
					}
				}

				if(songQueue.Count(x => x.source == sender) >= Config.Instance.request_limitPerUser) {
					sendResponse($"You already have {Config.Instance.request_limitPerUser} maps in the queue");

				} else if(songQueue.Contains(x => MapUtil.GetHashOfLevelid(x.levelId) == hash)) {
					sendResponse("The map is already in the queue");

				} else if(Config.Instance.queue_requeueLimit > 0 && songQueue.IsInHistory(levelId)) {
					sendResponse("The map has already been played recently");

				} else {
					int diff = -1;
					float? startTime = null;
					int validDiffs = -1;

					int GetValiDiffs(bool exitIfInvalid = true) {
						if(validDiffs == -1) {
							validDiffs = 0;

							if(mapNeedsDownload) {
								mapPool.SongdetailsFilterCheck(song, out validDiffs);
							} else {
								validDiffs = mapPool.filteredLevels[mapPool.requestableLevels[hash]].validDiffs;
							}
						}

						if(exitIfInvalid && validDiffs == 0)
							sendResponse("The map doesn't match the configured filters");

						return validDiffs;
					}

					if(split.Length > 2 && (Config.Instance.request_allowSpecificDiff || Config.Instance.request_allowSpecificTime)) {
						var m = diffTimePattern.Match(message);

						if(split.Length >= 4) {
							if(!m.Groups["timeM"].Success) {
								sendResponse("Invalid time (Ex: 2:33)");
								return null;
							} else if(!m.Groups["diff"].Success) {
								sendResponse("Invalid difficulty (Ex: 'hard' or 'ExpertPlus')");
								return null;
							}
						}

						if(
							Config.Instance.request_allowSpecificDiff &&
							m.Groups["diff"].Success &&
							Enum.TryParse<BeatmapDifficulty>(m.Groups["diff"].Value, true, out var requestedDiff)
						) {
							if(GetValiDiffs() == 0)
								return null;

							if(!ValidSong.IsDiffValid(GetValiDiffs(), requestedDiff)) {
								sendResponse($"The {requestedDiff} difficulty doesn't match the configured filters");
								return null;
							}

							diff = (int)requestedDiff;
						}

						if(
							Config.Instance.request_allowSpecificTime &&
							m.Groups["timeM"].Success &&
							m.Groups["timeS"].Success &&
							int.TryParse(m.Groups["timeM"].Value, out var timeM) &&
							int.TryParse(m.Groups["timeS"].Value, out var timeS)
						) {
							startTime = timeS + (timeM * 60);
						}
					}

					var theKeyAndPossiblyMapName = !Config.Instance.chat_request_show_name ? split[1] : $"{split[1]} - {song.songName}";

					if(mapNeedsDownload) {
						if(GetValiDiffs() == 0)
							return null;

						if(respondForDownload)
							sendResponse($"{theKeyAndPossiblyMapName} will be downloaded and queued when done");

						var dl = new SongDownloaderJob(song.mapId).Schedule();

						while(!dl.IsCompleted)
							Thread.Sleep(100);

						dl.Complete();

						if(!mapPool.LevelHashRequestable(hash)) {
							sendResponse("Map download failed");

							return null;
						}
					}

					var theMappe = mapPool.filteredLevels[mapPool.requestableLevels[hash]];

					if(diff == -1) {
						diff = (int)theMappe.GetRandomValidDiff();
					// Failsafe check if the above specific diff select worked off a songdetails check
					} else if(!theMappe.IsDiffValid((BeatmapDifficulty)diff)) {
						if(!mapNeedsDownload)
							sendResponse($"The {diff} difficulty doesn't match the configured filters");
						return null;
					}

					return new RequestResultSong() {
						shaffuruSong = new ShaffuruSong(levelId, diff, startTime, null, sender),
						requiredDownload = mapNeedsDownload,
						theKeyAndPossiblyMapName = theKeyAndPossiblyMapName
					};
				}

				return null;
			});
		}

		async void MainHandler(string sender, string message, string[] split, Action<string> sendResponse) {
			var ssong = await ProcessRequest(sender, message, split, sendResponse);

			if(ssong?.shaffuruSong == null)
				return;

			var queued = songQueue.EnqueueSong(ssong.shaffuruSong);

			if(queued) {
				if(!ssong.requiredDownload)
					sendResponse($"Queued {ssong.theKeyAndPossiblyMapName} ({(BeatmapDifficulty)ssong.shaffuruSong.diffIndex})");
			} else {
				sendResponse($"Couldn't queue {split[1]} (Unknown error)");
			}
		}
	}
}
