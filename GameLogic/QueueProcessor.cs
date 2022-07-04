using HarmonyLib;
using Shaffuru.AppLogic;
using SiraUtil.Zenject;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;
using static Shaffuru.AppLogic.SongQueueManager;

namespace Shaffuru.GameLogic {
	class QueueProcessor : IInitializable, ITickable {
		readonly MapPool mapPool;
		readonly BeatmapLoader beatmapLoader;
		readonly BeatmapSwitcher beatmapSwitcher;
		readonly SongQueueManager songQueueManager;
		readonly UBinder<Plugin, System.Random> rngSource;

		readonly AudioTimeSyncController audioTimeSyncController;


		static readonly FieldInfo FIELD_PauseMenuManager_InitData_previewBeatmapLevel = AccessTools.Field(typeof(PauseMenuManager.InitData), nameof(PauseMenuManager.InitData.previewBeatmapLevel));
		static readonly FieldInfo FIELD_PauseMenuManager_InitData_beatmapDifficulty = AccessTools.Field(typeof(PauseMenuManager.InitData), nameof(PauseMenuManager.InitData.beatmapDifficulty));
		readonly PauseMenuManager.InitData pauseMenuManager_InitData;


		readonly PlayedSongList playedSongList;

		public QueueProcessor(
			MapPool mapPool,
			BeatmapLoader beatmapLoader,
			BeatmapSwitcher beatmapSwitcher,
			SongQueueManager songQueueManager,
			AudioTimeSyncController audioTimeSyncController,
			PauseMenuManager.InitData pauseMenuManager_InitData,
			PlayedSongList playedSongList,
			UBinder<Plugin, System.Random> rngSource
		) {
			this.mapPool = mapPool;
			this.beatmapLoader = beatmapLoader;
			this.beatmapSwitcher = beatmapSwitcher;
			this.songQueueManager = songQueueManager;
			this.audioTimeSyncController = audioTimeSyncController;
			this.pauseMenuManager_InitData = pauseMenuManager_InitData;
			this.playedSongList = playedSongList;
			this.rngSource = rngSource;
		}

		public void Initialize() {
			//songQueueManager.EnqueueSong("custom_level_F402008042EFACA4291A6633EBB6B562E4ADCD87", BeatmapDifficulty.ExpertPlus, 5, 5);
			//songQueueManager.EnqueueSong("custom_level_64B81F99B76742B3C70EAC3BDA4230ED55E0D2AD", BeatmapDifficulty.Hard, 50, 4);
			//songQueueManager.EnqueueSong("custom_level_D77B25882287CD6CDF4E6D784BBE607C3295F79A", BeatmapDifficulty.Hard, 30, 5.5f);
			//songQueueManager.EnqueueSong("custom_level_9F783CE7F810062852795F4CBDF8335245FD044A", BeatmapDifficulty.ExpertPlus, 137);
		}

		public float switchToNextBeatmapAt = 1.3f;
		bool isQueueingNewSong = false;

		public async void Tick() {
			if(isQueueingNewSong || audioTimeSyncController.songTime < switchToNextBeatmapAt)
				return;

			// Dont queue a new song in the last 5 seconds... Kinda pointless
			if(audioTimeSyncController.songLength - audioTimeSyncController.songTime <= 5f)
				return;

			isQueueingNewSong = true;

			var queuedSong = songQueueManager.GetNextSong();

			if(queuedSong == null)
				return;

			IDifficultyBeatmap outDiff = null;
			IReadonlyBeatmapData outBeatmap = null;
			BeatmapLevelsModel.GetBeatmapLevelResult loadedBeatmap = default;

			await Task.Run(async () => {
				var diffIndex = queuedSong.diffIndex;

				if(diffIndex == -1) {
					var h = MapPool.GetHashOfLevelid(queuedSong.levelId);

					if(h == null)
						return;

					if(!mapPool.requestableLevels.TryGetValue(h, out var theMappe))
						return;

					diffIndex = (int)mapPool.filteredLevels[theMappe].GetRandomValidDiff();
				}

				try {
					loadedBeatmap = await beatmapLoader.LoadBeatmap(queuedSong.levelId);

					if(loadedBeatmap.isError)
						throw new Exception("isError");
				} catch(Exception ex) {
					Plugin.Log.Error(string.Format("Tried to queue {0} but failed to load it: {1}", queuedSong.levelId, ex));
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
					Plugin.Log.Error(string.Format("Tried to queue {0} but failed to find diff with index {1} and Standard characteristic", queuedSong.levelId, diffIndex));
					return;
				}

				try {
					outBeatmap = await beatmapLoader.TransformDifficulty(outDiff);
				} catch(Exception ex) {
					Plugin.Log.Error(string.Format("Tried to queue {0} but failed to transform beatmap: {1}", queuedSong.levelId, ex));
					return;
				}
			});

			if(outBeatmap == null) {
				isQueueingNewSong = false;
				return;
			}

			var songLength = outDiff.level.beatmapLevelData.audioClip.length - outDiff.level.songTimeOffset;
			var startTime = 0f;
			var length = songLength;

			if(Config.Instance.jumpcut_enabled) {
				if(queuedSong.length > 0) {
					length = Mathf.Clamp(queuedSong.length, Config.Instance.jumpcut_minSeconds, Config.Instance.jumpcut_maxSeconds);
				} else if(Config.Instance.jumpcut_maxSeconds > Config.Instance.jumpcut_minSeconds) {
					length = Config.Instance.jumpcut_minSeconds + (float)(rngSource.Value.NextDouble() * (Config.Instance.jumpcut_maxSeconds - Config.Instance.jumpcut_minSeconds));
				} else {
					length = Config.Instance.jumpcut_maxSeconds;
				}

				if(queuedSong.startTime < 0) {
					var min = songLength * .1f;
					var max = (songLength - length) * .9f;

					startTime = min + (float)(rngSource.Value.NextDouble() * (max - min));
				} else {
					startTime = queuedSong.startTime;
				}

				length = Mathf.Clamp(length, Math.Min(1, songLength), songLength);

				startTime = Mathf.Clamp(startTime, 0, songLength - length);
			}

			switchToNextBeatmapAt = audioTimeSyncController.songTime + length;

			beatmapSwitcher.SwitchToDifferentBeatmap(outDiff, outBeatmap, startTime, length);

			playedSongList.Add(new ShaffuruSong(queuedSong.levelId, outDiff.difficulty, startTime, length, queuedSong.source));

			FIELD_PauseMenuManager_InitData_previewBeatmapLevel.SetValue(pauseMenuManager_InitData, loadedBeatmap.beatmapLevel);
			FIELD_PauseMenuManager_InitData_beatmapDifficulty.SetValue(pauseMenuManager_InitData, outDiff.difficulty);

			isQueueingNewSong = false;
		}
	}
}
