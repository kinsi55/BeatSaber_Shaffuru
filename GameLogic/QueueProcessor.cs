using Shaffuru.AppLogic;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;
using static Shaffuru.AppLogic.SongQueueManager;

namespace Shaffuru.GameLogic {
	class QueueProcessor : IInitializable, ITickable {
		readonly BeatmapLoader beatmapLoader;
		readonly BeatmapSwitcher beatmapSwitcher;
		readonly SongQueueManager songQueueManager;
		readonly MapPool mapPool;

		readonly AudioTimeSyncController audioTimeSyncController;

		public QueueProcessor(
			BeatmapLoader beatmapLoader,
			BeatmapSwitcher beatmapSwitcher,
			SongQueueManager songQueueManager,
			MapPool mapPool,
			AudioTimeSyncController audioTimeSyncController,
			GameplayCoreSceneSetupData _sceneSetupData
		) {
			this.beatmapLoader = beatmapLoader;
			this.beatmapSwitcher = beatmapSwitcher;
			this.songQueueManager = songQueueManager;
			this.mapPool = mapPool;
			this.audioTimeSyncController = audioTimeSyncController;
		}

		public void Initialize() {
			//songQueueManager.EnqueueSong("custom_level_F402008042EFACA4291A6633EBB6B562E4ADCD87", BeatmapDifficulty.ExpertPlus, 5, 5);
			//songQueueManager.EnqueueSong("custom_level_64B81F99B76742B3C70EAC3BDA4230ED55E0D2AD", BeatmapDifficulty.Hard, 50, 4);
			//songQueueManager.EnqueueSong("custom_level_D77B25882287CD6CDF4E6D784BBE607C3295F79A", BeatmapDifficulty.Hard, 30, 5.5f);
			//songQueueManager.EnqueueSong("custom_level_9F783CE7F810062852795F4CBDF8335245FD044A", BeatmapDifficulty.ExpertPlus, 137);
		}

		public float switchToNextBeatmapAt = 1f;
		bool isQueueingNewSong = false;

		public async void Tick() {
			if(audioTimeSyncController.songTime < switchToNextBeatmapAt || isQueueingNewSong)
				return;

			isQueueingNewSong = true;

			var queuedSong = songQueueManager.DequeueSong();

			if(queuedSong == null) {
				if(!Config.Instance.queue_pickRandomSongIfEmpty)
					return;

				var levels = mapPool.filteredLevels.Where(x => !songQueueManager.history.Contains(x.level.levelID));

				// Shouldnt ever be the case, failsafe
				if(levels.Count() == 0)
					return;

				var x = levels.ElementAt(UnityEngine.Random.Range(0, levels.Count()));

				queuedSong = new QueuedSong(x.level.levelID, x.GetRandomValidDiff(), -1, -1, null);

				songQueueManager.history.Add(x.level.levelID);
			}

			IDifficultyBeatmap outDiff = null;
			IReadonlyBeatmapData outBeatmap = null;

			await Task.Run(async () => {
				BeatmapLevelsModel.GetBeatmapLevelResult loadedBeatmap;
				try {
					loadedBeatmap = await beatmapLoader.LoadBeatmap(queuedSong.levelId);

					if(loadedBeatmap.isError)
						throw new Exception("isError");
				} catch(Exception ex) {
					Plugin.Log.Error(string.Format("Tried to queue {0} but failed to load it: {1}", queuedSong.levelId, ex));
					return;
				}

				var diffIndex = queuedSong.diffIndex;

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
					outBeatmap = beatmapLoader.TransformDifficulty(outDiff);
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
			float startTime = 0;
			float length = songLength;

			if(Config.Instance.jumpcut_enabled) {
				if(queuedSong.startTime < 0) {
					startTime = UnityEngine.Random.Range(0, songLength);
				} else {
					startTime = queuedSong.startTime;
				}

				if(queuedSong.length > 0) {
					length = Mathf.Clamp(queuedSong.length, Config.Instance.jumpcut_minSeconds, Config.Instance.jumpcut_maxSeconds);
				} else {
					length = UnityEngine.Random.Range(Config.Instance.jumpcut_minSeconds, Config.Instance.jumpcut_maxSeconds);
				}

				length = Mathf.Clamp(length, 0, songLength);

				startTime = Mathf.Clamp(startTime, 0, songLength - length);
			}

			switchToNextBeatmapAt = audioTimeSyncController.songTime + length;

			// If the queue is otherwise empty, dont cap the last inserted's beatmap length, if pick random isnt active
			if(songQueueManager.IsEmpty() && !Config.Instance.queue_pickRandomSongIfEmpty)
				length = 0;

			beatmapSwitcher.SwitchToDifferentBeatmap(outDiff, outBeatmap, startTime, length);

			isQueueingNewSong = false;
		}
	}
}
