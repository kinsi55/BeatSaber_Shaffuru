using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Zenject;

namespace Shaffuru.GameLogic {
	class BeatmapSwitcher : IInitializable {
		static readonly FieldInfo FIELD_BasicBeatmapObjectManager_gameNotePoolContainer = AccessTools.Field(typeof(BasicBeatmapObjectManager), "_gameNotePoolContainer");
		static readonly FieldInfo FIELD_BasicBeatmapObjectManager_bombNotePoolContainer = AccessTools.Field(typeof(BasicBeatmapObjectManager), "_bombNotePoolContainer");
		static readonly FieldInfo FIELD_BasicBeatmapObjectManager_obstaclePoolContainer = AccessTools.Field(typeof(BasicBeatmapObjectManager), "_obstaclePoolContainer");

		static readonly FieldInfo FIELD_BeatmapObjectSpawnController_variableBpmProcessor = AccessTools.Field(typeof(BeatmapObjectSpawnController), "_variableBpmProcessor");
		static readonly FieldInfo FIELD_BeatmapObjectSpawnController_beatmapObjectSpawnMovementData = AccessTools.Field(typeof(BeatmapObjectSpawnController), "_beatmapObjectSpawnMovementData");

		static readonly FieldInfo FIELD_BeatmapObjectCallbackController_beatmapObjectCallbackData = AccessTools.Field(typeof(BeatmapObjectCallbackController), "_beatmapObjectCallbackData");
		static readonly FieldInfo FIELD_BeatmapObjectCallbackController_nextEventIndex = AccessTools.Field(typeof(BeatmapObjectCallbackController), "_nextEventIndex");

		static readonly FieldInfo FIELD_AudioTimeSyncController_audioLatency = AccessTools.Field(typeof(AudioTimeSyncController), "_audioLatency");

		static readonly FieldInfo FIELD_GameSongController_failAudioPitchGainEffect = AccessTools.Field(typeof(GameSongController), "_failAudioPitchGainEffect");

		static Action<BeatmapEventData, float> FIELD_BeatmapEventData_time_SETTER;


		readonly GameplayCoreSceneSetupData _sceneSetupData;

		readonly BeatmapObjectSpawnController.InitData BeatmapObjectSpawnController_InitData;

		readonly IReadonlyBeatmapData _readonlyBeatmapData;

		readonly BasicBeatmapObjectManager beatmapObjectManager;

		readonly BeatmapObjectSpawnController beatmapObjectSpawnController;
		readonly BeatmapObjectCallbackController beatmapObjectCallbackController;
		readonly AudioTimeSyncController audioTimeSyncController;
		readonly GameSongController gameSongController;

		RamCleaner ramCleaner;

		public BeatmapSwitcher(
			GameplayCoreSceneSetupData _sceneSetupData,
			BeatmapObjectSpawnController.InitData BeatmapObjectSpawnController_InitData,
			IReadonlyBeatmapData _readonlyBeatmapData,
			BasicBeatmapObjectManager beatmapObjectManager,
			BeatmapObjectSpawnController beatmapObjectSpawnController,
			BeatmapObjectCallbackController beatmapObjectCallbackController,
			AudioTimeSyncController audioTimeSyncController,
			GameSongController gameSongController
		) {
			this._sceneSetupData = _sceneSetupData;
			this.BeatmapObjectSpawnController_InitData = BeatmapObjectSpawnController_InitData;
			this._readonlyBeatmapData = _readonlyBeatmapData;
			this.beatmapObjectManager = beatmapObjectManager;
			this.beatmapObjectSpawnController = beatmapObjectSpawnController;
			this.beatmapObjectCallbackController = beatmapObjectCallbackController;
			this.audioTimeSyncController = audioTimeSyncController;
			this.gameSongController = gameSongController;

			if(FIELD_BeatmapEventData_time_SETTER == null) {
				var dm = new DynamicMethod("BeatmapEventData_time_SETTER", null, new[] { typeof(BeatmapEventData), typeof(float) });

				var il = dm.GetILGenerator(5);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Stfld, AccessTools.Field(typeof(BeatmapEventData), "time"));
				il.Emit(OpCodes.Ret);

				FIELD_BeatmapEventData_time_SETTER = (Action<BeatmapEventData, float>)dm.CreateDelegate(typeof(Action<BeatmapEventData, float>));
			}
		}

		public CustomSyncedAudioSource customAudioSource { get; private set; }

		MemoryPoolContainer<GameNoteController> _gameNotePoolContainer;
		MemoryPoolContainer<BombNoteController> _bombNotePoolContainer;
		MemoryPoolContainer<ObstacleController> _obstaclePoolContainer;

		List<BeatmapObjectCallbackData> BeatmapObjectCallbackData_nextObjectIndexInLine;

		VariableBpmProcessor variableBpmProcessor;
		BeatmapObjectSpawnMovementData beatmapObjectSpawnMovementData;

		public void Initialize() {
			ramCleaner = new RamCleaner();

			_gameNotePoolContainer = (MemoryPoolContainer<GameNoteController>)FIELD_BasicBeatmapObjectManager_gameNotePoolContainer.GetValue(beatmapObjectManager);
			_bombNotePoolContainer = (MemoryPoolContainer<BombNoteController>)FIELD_BasicBeatmapObjectManager_bombNotePoolContainer.GetValue(beatmapObjectManager);
			_obstaclePoolContainer = (MemoryPoolContainer<ObstacleController>)FIELD_BasicBeatmapObjectManager_obstaclePoolContainer.GetValue(beatmapObjectManager);

			// Get the array of "next" index for every line, because thats where we need to cut off / start off from later
			// I'm just gonna hope that this thing always only has one entry because else idk how tf it works lol
			BeatmapObjectCallbackData_nextObjectIndexInLine = ((List<BeatmapObjectCallbackData>)FIELD_BeatmapObjectCallbackController_beatmapObjectCallbackData
				.GetValue(beatmapObjectCallbackController));

			variableBpmProcessor = (VariableBpmProcessor)FIELD_BeatmapObjectSpawnController_variableBpmProcessor.GetValue(beatmapObjectSpawnController);
			beatmapObjectSpawnMovementData = (BeatmapObjectSpawnMovementData)FIELD_BeatmapObjectSpawnController_beatmapObjectSpawnMovementData.GetValue(beatmapObjectSpawnController);

			var latency = (FloatSO)FIELD_AudioTimeSyncController_audioLatency.GetValue(audioTimeSyncController);

			customAudioSource = new CustomSyncedAudioSource(audioTimeSyncController, latency.value);
			((AudioPitchGainEffect)FIELD_GameSongController_failAudioPitchGainEffect.GetValue(gameSongController)).SetAudioSource(customAudioSource.source);

			// We dont need that to play
			audioTimeSyncController.audioSource.mute = true;
			Plugin.Log.Debug("BeatmapSwitcher loaded");
		}

		void UpdateBeatmapObjectSpawnControllerInitData(float bpm, float njs, float offset) {
			variableBpmProcessor.SetBpm(bpm);
			beatmapObjectSpawnMovementData.Init(
				// If this changes the game would probably explode
				BeatmapObjectSpawnController_InitData.noteLinesCount,
				njs,
				bpm,
				offset,
				// This should be fixed, based off player height
				BeatmapObjectSpawnController_InitData.jumpOffsetY,
				// This (probably?) wont work with 360 - too bad!
				Vector3.right,
				Vector3.forward
			);
		}

		public void SwitchToDifferentBeatmap(IDifficultyBeatmap difficultyBeatmap, IReadonlyBeatmapData replacementBeatmapData, float startTime, float lengthLimit = 0) {
			audioTimeSyncController.StartCoroutine(Switcher(difficultyBeatmap, replacementBeatmapData, startTime, lengthLimit));
		}

		IEnumerator Switcher(IDifficultyBeatmap replacementDifficultyBeatmap, IReadonlyBeatmapData replacementBeatmapData, float startTime, float lengthLimit) {
			// Get the "spawn ahead" time the current song uses - These are notes which are spawned / moving in
			// before the specific song location has been reached
			var jDuration = beatmapObjectSpawnController.jumpDuration;
			var mDuration = beatmapObjectSpawnController.moveDuration;

			var dissolveTime = (mDuration * 0.2f) / audioTimeSyncController.audioSource.pitch;

			// We are switching to the new map... Dissolve everything thats active right now
			foreach(var x in _gameNotePoolContainer.activeItems)
				x.Dissolve(dissolveTime);
			foreach(var x in _bombNotePoolContainer.activeItems)
				x.Dissolve(dissolveTime);
			foreach(var x in _obstaclePoolContainer.activeItems)
				x.Dissolve(dissolveTime);

			if(!ramCleaner.TrySkip()) {
				yield return new WaitForSecondsRealtime(dissolveTime * 0.8f);
				customAudioSource.SetAudio(null);

				audioTimeSyncController.audioSource.Pause();

				yield return new WaitForSecondsRealtime(dissolveTime * 0.2f);
				yield return ramCleaner.ClearRam();

				// Make sure we have had at least 20 frames, not just half a second
				audioTimeSyncController.audioSource.UnPause();
				var s = audioTimeSyncController.audioSource.time;
				for(var i = 0; i < 20; i++)
					yield return null;
				yield return new WaitUntil(() => audioTimeSyncController.audioSource.time - s >= 0.3f);
			}


			// Force this to execute after Behaviour Update()'s so the TimeSyncController is up-to-date
			yield return null;

			// Yeet everything from the beatmap, starting from the index the map is being currently processed from
			// This is a MASSIVE amount of hack

			var currentAudioTime = audioTimeSyncController.songTime;
			var reactionTime = Config.Instance.jumpcut_reactionTime;

			var idkman = BeatmapObjectCallbackData_nextObjectIndexInLine[0].nextObjectIndexInLine;

			for(var lineIndex = 0; lineIndex < BeatmapObjectSpawnController_InitData.noteLinesCount; lineIndex++) {
				// Readonly? I dont think so.
				var line = (List<BeatmapObjectData>)_readonlyBeatmapData.beatmapLinesData[lineIndex].beatmapObjectsData;
				var replacementLine = replacementBeatmapData.beatmapLinesData[lineIndex].beatmapObjectsData;

				int objInsertIndex = idkman[lineIndex];

				for(var i = 0; i < replacementLine.Count; i++) {
					var obj = replacementLine[i];
					// We only wanna insert items which would be at least moveDuration time away

					//TODO: for obstacles, if the start time is in the past, but the end time isnt, move it accordingly so its still shown
					var x = obj.time - startTime - jDuration;
					if(x < 0f)
						continue;

					if(lengthLimit > 0 && x > lengthLimit + 1)
						break;

					obj.MoveTime(x + currentAudioTime + reactionTime);

					if(objInsertIndex < line.Count) {
						line[objInsertIndex] = obj;
					} else {
						line.Add(obj);
					}
					objInsertIndex++;
				}

				/*
				 * If something from the previous song was not overwritten, yeet it.
				 * This is probably the lowest allocation implementation possible
				 */
				for(var i = objInsertIndex; i < line.Count; i++)
					line[i].MoveTime(float.MaxValue);
			}


			// Time to fix the events too 💀

			var events = (List<BeatmapEventData>)_readonlyBeatmapData.beatmapEventsData;
			var replacementEvents = replacementBeatmapData.beatmapEventsData;

			int eventInsertIndex = (int)FIELD_BeatmapObjectCallbackController_nextEventIndex.GetValue(beatmapObjectCallbackController);

			for(var i = 0; i < replacementBeatmapData.beatmapEventsData.Count; i++) {
				var obj = replacementEvents[i];

				var x = obj.time - startTime - jDuration;
				if(x < 0f)
					continue;

				if(lengthLimit > 0 && x > lengthLimit + 1)
					break;

				// readonly 😡
				FIELD_BeatmapEventData_time_SETTER(obj, x + currentAudioTime + reactionTime);

				if(eventInsertIndex < events.Count) {
					events[eventInsertIndex] = obj;
				} else {
					events.Add(obj);
				}
				eventInsertIndex++;
			}

			for(var i = eventInsertIndex; i < events.Count; i++)
				FIELD_BeatmapEventData_time_SETTER(events[i], float.MaxValue);


			// Finally, update to the new maps stuffs
			var njs = replacementDifficultyBeatmap.noteJumpMovementSpeed;
			if(njs == 0)
				njs = BeatmapDifficultyMethods.NoteJumpMovementSpeed(replacementDifficultyBeatmap.difficulty);

			UpdateBeatmapObjectSpawnControllerInitData(replacementDifficultyBeatmap.level.beatsPerMinute, njs, replacementDifficultyBeatmap.noteJumpStartBeatOffset);

			var timePre = audioTimeSyncController.songTime;
			// Wait before swapping in the new audio until the new notes are a bit closer. Feels better
			yield return new WaitUntil(() => audioTimeSyncController.songTime - timePre >= Math.Min(reactionTime * 0.5f, startTime));

			// New audio
			// This is where I would go ahead and fiddle with the AudioTimeSync controller and swap out the audio clip etc
			// But that would be a MASSIVE pain, so why dont we just create our own audioclip and sync that to the normal sync controller? :)
			customAudioSource.SetAudio(replacementDifficultyBeatmap.level.beatmapLevelData.audioClip, startTime + jDuration - reactionTime + (audioTimeSyncController.songTime - timePre));
		}
	}

	class CustomSyncedAudioSource {
		public AudioSource source { get; private set; }
		public float audioLatency { get; private set; } = 0f;

		public CustomSyncedAudioSource(AudioTimeSyncController controller, float audioLatency) {
			// Easiest way to keep same Volume etc
			var x = GameObject.Instantiate(controller, controller.transform);

			foreach(var l in x.gameObject.GetComponents<MonoBehaviour>())
				if(l.name != "AudioSource")
					GameObject.Destroy(l);

			foreach(var child in x.transform.Cast<Transform>())
				GameObject.Destroy(child.gameObject);

			source = x.GetComponent<AudioSource>();
			this.audioLatency = audioLatency;
		}

		public void SetAudio(AudioClip clip, float songStart = 0) {
			source.Stop();
			//source.clip?.UnloadAudioData();
			source.time = 0f;
			source.clip = clip;
			if(clip != null) {
				source.time = songStart + audioLatency;
				source.Play();
				//Console.WriteLine("{0} {1}", source.isPlaying, source.clip.length);
			}
		}
	}
}
