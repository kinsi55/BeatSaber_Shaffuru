using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Shaffuru.Util;
using Unity.Jobs;
using Zenject;

namespace Shaffuru.AppLogic {
	class RequestManager : IInitializable, IDisposable {
		readonly SongQueueManager songQueueManager;

		readonly MapPool mapPool;
		readonly IChatMessageSource chatSource;

		public RequestManager(SongQueueManager songQueueManager, MapPool mapPool, IChatMessageSource chatSource) {
			this.songQueueManager = songQueueManager;
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

		void Msg(string message, object channel) => chatSource.SendChatMessage($"! {message}", channel);

		static Regex diffTimePattern = new Regex(@"(?<diff>Easy|Normal|Hard|Expert|ExpertPlus)?\s*((?<timeM>[0-9]{1,2}):(?<timeS>[0-5]?[0-9])|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private void Twitch_OnTextMessageReceived(string sender, string message, object channel) {
			if(!Config.Instance.chat_request_enabled || (!message.StartsWith("!chaos", StringComparison.OrdinalIgnoreCase) && !message.StartsWith("!sr", StringComparison.OrdinalIgnoreCase)))
				return;

			if(mapPool.filteredLevels == null || SongDetailsUtil.instance == null) {
				Msg($"@{sender} Shaffuru is not initialized", channel);
				return;
			}

			Task.Run(() => {
				var split = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				if(split.Length < 2)
					return;

				if(songQueueManager.IsFull()) {
					Msg($"@{sender} The queue is full", channel);
					return;
				}

				var diff = -1;
				var startTime = -1;
				string hash = null;
				string levelId = null;
				var song = SongDetailsCache.Structs.Song.none;

				// https://github.com/kinsi55/BeatSaber_SongDetails/commit/7c85cee7849794c8670ef960bc6a583ba9c68e9c 💀
				var key = split[1].ToLower();
				if(key.Length < 10) {
					try {
						if(SongDetailsUtil.instance.songs.FindByMapId(key, out song)) {
							hash = song.hash;
							levelId = $"custom_level_{hash}";
						}
					} catch { }
				}

				bool mapNeedsDownload = false;

				if(hash == null) {
					Msg($"@{sender} Unknown map ID", channel);

					return;
				} else if(!mapPool.LevelHashRequestable(hash)) {
					if(SongCore.Collections.hashForLevelID(levelId) != string.Empty) {
						Msg($"@{sender} The map does not match the configured filters", channel);
						return;
					} else if(!Config.Instance.request_allowDownloading) {
						Msg($"@{sender} The map is not downloaded", channel);
						return;
					} else if(SongDownloaderJob.downloadingMaps.Contains(song.mapId)) {
						Msg($"@{sender} The map is currently being downloaded", channel);
						return;
					} else {
						mapNeedsDownload = true;
					}
				}


				if(songQueueManager.Count(x => x.source == sender) >= Config.Instance.request_limitPerUser) {
					Msg($"@{sender} You already have {Config.Instance.request_limitPerUser} maps in the queue", channel);

				} else if(songQueueManager.Contains(x => MapPool.GetHashOfLevelid(x.levelId) == hash)) {
					Msg($"@{sender} This map is already in the queue", channel);

				} else if(Config.Instance.queue_requeueLimit > 0 && songQueueManager.IsInHistory(levelId)) {
					Msg($"@{sender} The map has already been played recently", channel);

				} else {
					if(mapNeedsDownload) {
						if(!mapPool.SongdetailsFilterCheck(song, out var _)) {
							Msg($"@{sender} The map does not match the configured filters", channel);
							return;
						}

						Msg($"@{sender} The map will be downloaded and queued when done", channel);

						var dl = new SongDownloaderJob(song.mapId).Schedule();

						while(!dl.IsCompleted)
							Thread.Sleep(100);

						dl.Complete();

						if(!mapPool.LevelHashRequestable(hash)) {
							Msg($"@{sender} Map download failed", channel);

							return;
						}
					}

					var theMappe = mapPool.filteredLevels[mapPool.requestableLevels[hash]];

					if(split.Length > 2 && (Config.Instance.request_allowSpecificDiff || Config.Instance.request_allowSpecificTime)) {
						var m = diffTimePattern.Match(message);

						if(split.Length >= 4) {
							if(!m.Groups["timeM"].Success) {
								Msg($"@{sender} Invalid time (Ex: 2:33)", channel);
								return;
							} else if(!m.Groups["diff"].Success) {
								Msg($"@{sender} Invalid difficulty (Ex: 'hard' or 'ExpertPlus')", channel);
								return;
							}
						}

						if(
							Config.Instance.request_allowSpecificDiff &&
							m.Groups["diff"].Success &&
							Enum.TryParse<BeatmapDifficulty>(m.Groups["diff"].Value, true, out var requestedDiff)
						) {
							if(!theMappe.IsDiffValid(requestedDiff)) {
								Msg($"@{sender} The {requestedDiff} difficulty does not match the configured filters", channel);
								return;
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

					if(diff == -1)
						diff = (int)theMappe.GetRandomValidDiff();

					var queued = songQueueManager.EnqueueSong(new ShaffuruSong(
						levelId,
						diff,
						startTime,
						-1,
						sender
					));

					if(queued) {
						if(Config.Instance.chat_request_show_name) {
							Msg($"@{sender} Queued {split[1]} - {song.songName} ({(BeatmapDifficulty)diff})", channel);
						} else {
							Msg($"@{sender} Queued {split[1]} ({(BeatmapDifficulty)diff})", channel);
						}
					} else {
						Msg($"@{sender} Couldn't queue {split[1]} (Unknown error)", channel);
					}
				}
			});
		}
	}
}
