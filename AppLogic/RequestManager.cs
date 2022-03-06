using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CatCore;
using CatCore.Models.Twitch;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Twitch.Interfaces;
using Zenject;
using static Shaffuru.AppLogic.SongQueueManager;

namespace Shaffuru.AppLogic {
	class RequestManager : IInitializable, IDisposable {
		static CatCoreInstance catCore;
		static ITwitchService twitch;
		SongQueueManager songQueueManager;

		MapPool mapPool;

		public RequestManager(SongQueueManager songQueueManager, MapPool mapPool) {
			this.songQueueManager = songQueueManager;
			this.mapPool = mapPool;
		}

		public void Initialize() {
			catCore ??= CatCoreInstance.Create();

			twitch ??= catCore.RunTwitchServices();

			twitch.OnTextMessageReceived += Twitch_OnTextMessageReceived;
		}

		public void Dispose() {
			twitch.OnTextMessageReceived -= Twitch_OnTextMessageReceived;
		}

		void Msg(string message, TwitchChannel channel) {
			channel.SendMessage($"! {message}");
		}

		static Regex diffTimePattern = new Regex(@"(?<diff>Easy|Normal|Hard|Expert|ExpertPlus)?\s*((?<timeM>[0-9]{1,2}):(?<timeS>[0-5]?[0-9])|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private void Twitch_OnTextMessageReceived(ITwitchService __, TwitchMessage message) {
			if(!Config.Instance.chat_request_enabled || (!message.Message.StartsWith("!chaos") && !message.Message.StartsWith("!sr")))
				return;

			var sender = message.Sender.UserName;

			if(mapPool.filteredLevels == null) {
				Msg($"@{sender} Shaffuru is not initialized", message.Channel);
				return;
			}

			Task.Run(() => {
				var split = message.Message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				if(split.Length < 2)
					return;

				if(songQueueManager.IsFull()) {
					Msg($"@{sender} The queue is full", message.Channel);
					return;
				}

				var diff = -1;
				var startTime = -1;
				string hash = null;

				// https://github.com/kinsi55/BeatSaber_SongDetails/commit/7c85cee7849794c8670ef960bc6a583ba9c68e9c 💀
				var key = split[1].ToLower();
				if(key.Length < 10) {
					try {
						hash = mapPool.GetHashFromBeatsaverId(key);
					} catch { }
				}

				if(hash == null) {
					Msg($"@{sender} Unknown map ID", message.Channel);

				} else if(!mapPool.HasLevelId(hash)) {
					Msg($"@{sender} The map is not downloaded or does not match the configured filters", message.Channel);

				} else if(songQueueManager.Count(x => x.source == sender) >= Config.Instance.request_limitPerUser) {
					Msg($"@{sender} You already have {Config.Instance.request_limitPerUser} maps in the queue", message.Channel);

				} else if(songQueueManager.Contains(x => MapPool.GetHashOfLevelid(x.levelId) == hash)) {
					Msg($"@{sender} This map is already in the queue", message.Channel);

				} else if(Config.Instance.queue_requeueLimit > 0 && songQueueManager.IsInHistory($"custom_level_{hash}")) {
					Msg($"@{sender} The map has already been played recently", message.Channel);

				} else {
					var theMappe = mapPool.filteredLevels[mapPool.requestableLevels[hash]];

					if(split.Length > 2 && (Config.Instance.request_allowSpecificDiff || Config.Instance.request_allowSpecificTime)) {
						var m = diffTimePattern.Match(message.Message);

						if(split.Length >= 4) {
							if(!m.Groups["timeM"].Success) {
								Msg($"@{sender} Invalid time (Ex: 2:33)", message.Channel);
								return;
							} else if(!m.Groups["diff"].Success) {
								Msg($"@{sender} Invalid difficulty (Ex: 'hard' or 'ExpertPlus')", message.Channel);
								return;
							}
						}

						if(
							Config.Instance.request_allowSpecificDiff &&
							m.Groups["diff"].Success &&
							Enum.TryParse<BeatmapDifficulty>(m.Groups["diff"].Value, true, out var requestedDiff)
						) {
							if(!theMappe.IsDiffValid(requestedDiff)) {
								Msg($"@{sender} The {requestedDiff} difficulty does not match the configured filters", message.Channel);
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
						$"custom_level_{hash}",
						diff,
						startTime,
						-1,
						sender
					));

					if(queued) {
						Msg($"@{sender} Queued {split[1]} - {theMappe.level.songName} ({(BeatmapDifficulty)diff})", message.Channel);
					} else {
						Msg($"@{sender} Couldn't queue map (Unknown error)", message.Channel);
					}
				}
			});
		}
	}
}
