using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using Shaffuru.AppLogic;
using Shaffuru.Util;
using SiraUtil.Zenject;
using UnityEngine;
using Zenject;

namespace Shaffuru.GameLogic {
	class QueueProcessor : ITickable {
		readonly MapPool mapPool;
		readonly BeatmapLoader beatmapLoader;
		readonly BeatmapSwitcher beatmapSwitcher;
		readonly ISongQueueManager songQueueManager;
		readonly UBinder<Plugin, System.Random> rngSource;

		readonly AudioTimeSyncControllerWrapper audioTimeSyncControllerWrapper;


		static readonly FieldInfo FIELD_PauseMenuManager_InitData_previewBeatmapLevel = AccessTools.Field(typeof(PauseMenuManager.InitData), nameof(PauseMenuManager.InitData.previewBeatmapLevel));
		static readonly FieldInfo FIELD_PauseMenuManager_InitData_beatmapDifficulty = AccessTools.Field(typeof(PauseMenuManager.InitData), nameof(PauseMenuManager.InitData.beatmapDifficulty));
		readonly PauseMenuManager.InitData pauseMenuManager_InitData;


		readonly PlayedSongList playedSongList;

		public QueueProcessor(
			MapPool mapPool,
			BeatmapLoader beatmapLoader,
			BeatmapSwitcher beatmapSwitcher,
			ISongQueueManager songQueueManager,
			AudioTimeSyncControllerWrapper audioTimeSyncControllerWrapper,
			[InjectOptional] PauseMenuManager.InitData pauseMenuManager_InitData,
			PlayedSongList playedSongList,
			UBinder<Plugin, System.Random> rngSource
		) {
			this.mapPool = mapPool;
			this.beatmapLoader = beatmapLoader;
			this.beatmapSwitcher = beatmapSwitcher;
			this.songQueueManager = songQueueManager;
			this.audioTimeSyncControllerWrapper = audioTimeSyncControllerWrapper;
			this.pauseMenuManager_InitData = pauseMenuManager_InitData;
			this.playedSongList = playedSongList;
			this.rngSource = rngSource;

			//songQueueManager.EnqueueSong("custom_level_F402008042EFACA4291A6633EBB6B562E4ADCD87", BeatmapDifficulty.ExpertPlus, 5, 5);
		}

		public float switchToNextBeatmapAt = 1.3f;
		bool isExecutingSwitch = false;

		public event Action<ShaffuruSong> switchedToNewSong;

		public async void Tick() {
			if(isExecutingSwitch || audioTimeSyncControllerWrapper.songTime < switchToNextBeatmapAt)
				return;

			// Dont queue a new song in the last 5 seconds... Kinda pointless
			if(audioTimeSyncControllerWrapper.songLength - audioTimeSyncControllerWrapper.songTime <= 5f)
				return;

			isExecutingSwitch = true;

			var queuedSong = songQueueManager.GetNextSong();

			if(queuedSong != null)
				await SwitchToNewSong(queuedSong);

			isExecutingSwitch = false;
		}

		public async Task SwitchToNewSong(ShaffuruSong song) {
			IDifficultyBeatmap outDiff = null;
			IReadonlyBeatmapData outBeatmap = null;
			BeatmapLevelsModel.GetBeatmapLevelResult loadedBeatmap = default;

			await Task.Run(async () => {
				var diffIndex = song.diffIndex;

				if(diffIndex == -1) {
					var h = MapUtil.GetHashOfLevelid(song.levelId);

					if(h == null)
						return;

					if(!mapPool.requestableLevels.TryGetValue(h, out var theMappe))
						return;

					diffIndex = (int)mapPool.filteredLevels[theMappe].GetRandomValidDiff();
				}

				try {
					loadedBeatmap = await beatmapLoader.LoadBeatmap(song.levelId);

					if(loadedBeatmap.isError)
						throw new Exception("isError");
				} catch(Exception ex) {
					Plugin.Log.Error(string.Format("Tried to queue {0} but failed to load it: {1}", song.levelId, ex));
					return;
				}

				foreach(var d in loadedBeatmap.beatmapLevel.beatmapLevelData.difficultyBeatmapSets) {
					if(d.beatmapCharacteristic.serializedName != "Standard")
						continue;

					foreach(var diff in d.difficultyBeatmaps) {
						if((int)diff.difficulty != diffIndex)
							continue;

						outDiff = diff;
						break;
					}
					break;
				}

				if(outDiff == null) {
					Plugin.Log.Error(string.Format("Tried to queue {0} but failed to find diff with index {1} and Standard characteristic", song.levelId, diffIndex));
					return;
				}

				try {
					outBeatmap = await beatmapLoader.TransformDifficulty(outDiff);
				} catch(Exception ex) {
					Plugin.Log.Error(string.Format("Tried to queue {0} but failed to transform beatmap: {1}", song.levelId, ex));
					return;
				}
			});

			if(outBeatmap == null)
				return;

			var songLength = outDiff.level.beatmapLevelData.audioClip.length - outDiff.level.songTimeOffset;
			var startTime = song.startTime ?? 0f;
			var length = song.length ?? songLength;

			// TODO: If / when I should ever set song.length somewhere I need to handle this in a better way
			if(Config.Instance.jumpcut_enabled && song.length == null) {
				if(song.length > 0) {
					length = Mathf.Clamp(song.length ?? 0, Config.Instance.jumpcut_minSeconds, Config.Instance.jumpcut_maxSeconds);
				} else if(Config.Instance.jumpcut_maxSeconds > Config.Instance.jumpcut_minSeconds) {
					length = Config.Instance.jumpcut_minSeconds + (float)(rngSource.Value.NextDouble() * (Config.Instance.jumpcut_maxSeconds - Config.Instance.jumpcut_minSeconds));
				} else {
					length = Config.Instance.jumpcut_maxSeconds;
				}

				if(song.startTime == null) {
					var min = songLength * .1f;
					var max = (songLength - length) * .9f;

					startTime = min + (float)(rngSource.Value.NextDouble() * (max - min));
				}

				length = Mathf.Clamp(length, Math.Min(1, songLength), songLength);

				startTime = Mathf.Clamp(startTime, 0, songLength - length);
			}

			switchToNextBeatmapAt = audioTimeSyncControllerWrapper.songTime + length;

			var playedSong = new ShaffuruSong(song.levelId, outDiff.difficulty, startTime, length, song.source);

			beatmapSwitcher.SwitchToDifferentBeatmap(outDiff, outBeatmap, startTime, length);

			playedSongList.Add(playedSong);

			if(pauseMenuManager_InitData != null) {
				FIELD_PauseMenuManager_InitData_previewBeatmapLevel.SetValue(pauseMenuManager_InitData, loadedBeatmap.beatmapLevel);
				FIELD_PauseMenuManager_InitData_beatmapDifficulty.SetValue(pauseMenuManager_InitData, outDiff.difficulty);
			}

			switchedToNewSong?.Invoke(playedSong);
		}
	}
}
