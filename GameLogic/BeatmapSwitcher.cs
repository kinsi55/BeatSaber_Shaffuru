using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Shaffuru.HarmonyPatches;
using UnityEngine;
using Zenject;

namespace Shaffuru.GameLogic {
	class BeatmapSwitcher : IDisposable {
		static readonly FieldInfo FIELD_BeatmapObjectSpawnController_beatmapObjectSpawnMovementData 
			= AccessTools.Field(typeof(BeatmapObjectSpawnController), "_beatmapObjectSpawnMovementData");
		static readonly FieldInfo FIELD_BeatmapObjectSpawnController_disableSpawning
			= AccessTools.Field(typeof(BeatmapObjectSpawnController), "_disableSpawning");

		static readonly FieldInfo FIELD_BeatmapObjectCallbackController_callbacksInTimes = AccessTools.Field(typeof(BeatmapCallbacksController), "_callbacksInTimes");

		static readonly IPA.Utilities.FieldAccessor<CallbacksInTime, Dictionary<Type, List<BeatmapDataCallbackWrapper>>>.Accessor FIELD_CallbacksInTime_callbacks
			= IPA.Utilities.FieldAccessor<CallbacksInTime, Dictionary<Type, List<BeatmapDataCallbackWrapper>>>.GetAccessor("_callbacks");


		static readonly MethodInfo SETTER_GameEnergyCounter_noFail = AccessTools.PropertySetter(typeof(GameEnergyCounter), nameof(GameEnergyCounter.noFail));

		static readonly IPA.Utilities.FieldAccessor<BeatmapDataItem, float>.Accessor SETTER_BeatmapDataItem_time 
			= IPA.Utilities.FieldAccessor<BeatmapDataItem, float>.GetAccessor($"<{nameof(BeatmapDataItem.time)}>k__BackingField");
		static readonly IPA.Utilities.FieldAccessor<SliderData, float>.Accessor SETTER_SliderData_tailTime 
			= IPA.Utilities.FieldAccessor<SliderData, float>.GetAccessor($"<{nameof(SliderData.tailTime)}>k__BackingField");

		static readonly IPA.Utilities.FieldAccessor<BeatmapDataCallbackWrapper, float>.Accessor SETTER_BeatmapDataCallbackWrapper_aheadTime 
			= IPA.Utilities.FieldAccessor<BeatmapDataCallbackWrapper, float>.GetAccessor("aheadTime");

		readonly GameplayCoreSceneSetupData sceneSetupData;

		readonly float startBeatmapCallbackAheadTime;
		readonly IJumpOffsetYProvider jumpOffsetYProvider;

		readonly IReadonlyBeatmapData readonlyBeatmapData;
		readonly BeatmapObjectSpawnController beatmapObjectSpawnController;

		readonly BeatmapCallbacksController beatmapCallbacksController;
		readonly IReadOnlyDictionary<float, CallbacksInTime> beatmapObjectCallbackController_callbacksInTimes;
		readonly AudioTimeWrapper audioTimeWrapper;
		readonly GameEnergyCounter gameEnergyCounter;

		public readonly CustomAudioSource customAudioSource;

		readonly BeatmapObjectSpawnMovementData beatmapObjectSpawnMovementData;

		readonly RamCleaner ramCleaner;

		public BeatmapSwitcher(
			GameplayCoreSceneSetupData sceneSetupData,
			[InjectOptional] IJumpOffsetYProvider jumpOffsetYProvider,
			[InjectOptional] IReadonlyBeatmapData readonlyBeatmapData,
			[InjectOptional] BeatmapObjectSpawnController beatmapObjectSpawnController,
			[InjectOptional] BeatmapCallbacksController beatmapCallbacksController,
			AudioTimeWrapper audioTimeWrapper,
			[InjectOptional] GameEnergyCounter gameEnergyCounter,
			CustomAudioSource customAudioSource,
			RamCleaner ramCleaner
		) {
			this.sceneSetupData = sceneSetupData;
			this.jumpOffsetYProvider = jumpOffsetYProvider;
			this.readonlyBeatmapData = readonlyBeatmapData;
			this.beatmapObjectSpawnController = beatmapObjectSpawnController;
			this.beatmapCallbacksController = beatmapCallbacksController;
			this.audioTimeWrapper = audioTimeWrapper;
			this.customAudioSource = customAudioSource;
			this.gameEnergyCounter = gameEnergyCounter;
			this.ramCleaner = ramCleaner;

			if(beatmapCallbacksController != null)
				beatmapObjectCallbackController_callbacksInTimes = (Dictionary<float, CallbacksInTime>)FIELD_BeatmapObjectCallbackController_callbacksInTimes.GetValue(beatmapCallbacksController);

			if(beatmapObjectSpawnController != null) {
				beatmapObjectSpawnMovementData = (BeatmapObjectSpawnMovementData)FIELD_BeatmapObjectSpawnController_beatmapObjectSpawnMovementData.GetValue(beatmapObjectSpawnController);
				startBeatmapCallbackAheadTime = beatmapObjectSpawnMovementData.spawnAheadTime;
			}
		}

		void UpdateBeatmapObjectSpawnControllerInitData(float bpm, float njs, float mapOffset, int noteLinesCount) {
			BeatmapObjectSpawnControllerHelpers.GetNoteJumpValues(sceneSetupData.playerSpecificSettings, mapOffset, out var noteJumpValueType, out var noteJumpValue);
			beatmapObjectSpawnMovementData.Init(
				// If this changes the game would probably explode - I dont think it does ever tho?
				noteLinesCount,
				njs,
				bpm,
				noteJumpValueType,
				noteJumpValue,
				jumpOffsetYProvider,
				// This (probably?) wont work with 360 - too bad!
				Vector3.right,
				Vector3.forward
			);
			beatmapCallbacksController.TriggerBeatmapEvent(new BPMChangeBeatmapEventData(0, bpm));
		}

		void SetSpawningEnabled(bool enable) {
			if(beatmapObjectSpawnController != null)
				FIELD_BeatmapObjectSpawnController_disableSpawning.SetValue(beatmapObjectSpawnController, !enable);
		}

		public void SwitchToDifferentBeatmap(IDifficultyBeatmap difficultyBeatmap, IReadonlyBeatmapData replacementBeatmapData, float startTime, float secondsToInsert = 0) {
			SharedCoroutineStarter.instance.StartCoroutine(NoSpawnZone());

			IEnumerator NoSpawnZone() {
				SetSpawningEnabled(false);
				HeckOffCutSoundsCrash.enablePatch = true;

				yield return Switcher(difficultyBeatmap, replacementBeatmapData, startTime, secondsToInsert);

				HeckOffCutSoundsCrash.enablePatch = false;
				SetSpawningEnabled(true);
			}
		}

		IEnumerator Switcher(IDifficultyBeatmap replacementDifficultyBeatmap, IReadonlyBeatmapData replacementBeatmapData, float startTime, float secondsToInsert = 0) {
			var reactionTime = Config.Instance.transition_reactionTime;
			var dissolveTime = reactionTime * 0.6f;

			BeatmapObjectDissolver.DissolveAllAndEverything(dissolveTime);

			if(!ramCleaner.TrySkip() && audioTimeWrapper.songLength - audioTimeWrapper.songTime >= 30f) {
				yield return new WaitForSecondsRealtime(dissolveTime * 0.8f);
				
				audioTimeWrapper.Pause();

				customAudioSource.SetClip(replacementDifficultyBeatmap.level.beatmapLevelData.audioClip);

				yield return new WaitForSecondsRealtime(dissolveTime * 0.2f);
				yield return ramCleaner.ClearRam();

				// Make sure we have had at least 20 frames, not just half a second
				var s = Time.time;
				for(var i = 0; i < 20; i++)
					yield return null;
				yield return new WaitUntil(() => !audioTimeWrapper.isValid || Time.time - s >= 0.5f);
				audioTimeWrapper.Resume();
			} else {
				// Force this to execute after Behaviour Update()'s so the TimeSyncController is up-to-date
				yield return null;
			}

			if(!audioTimeWrapper.isValid)
				yield break;

			var currentAudioTime = audioTimeWrapper.songTime;


			if(readonlyBeatmapData != null) {
				LinkedListNode<BeatmapDataItem> prevNode = null;

				// Find the newest node processed by any callback (biggest aheadTime)
				foreach(var x in beatmapObjectCallbackController_callbacksInTimes) {
					if(prevNode == null || prevNode.Value.time < x.Value.lastProcessedNode.Value.time)
						prevNode = x.Value.lastProcessedNode.Next;
				}

				foreach(var x in replacementBeatmapData.allBeatmapDataItems) {
					var relativeSongTime = x.time - startTime;

					// If we are past the time which we want to add / replace in, break
					if(secondsToInsert > 0 && relativeSongTime > secondsToInsert + 1)
						break;

					// If the current nodes (start) is in the past...
					if(relativeSongTime < 0) {
						// Ignore it if its not a wall
						if(x.type != BeatmapDataItem.BeatmapDataItemType.BeatmapObject || !(x is ObstacleData woll))
							continue;

						// If it is a wall, modify the start time and length so that its start is `reactionTime`ms in the future
						var newLength = woll.duration + relativeSongTime - reactionTime;

						if(newLength < 0)
							continue;

						woll.UpdateDuration(newLength);

						relativeSongTime = reactionTime;
					}

					// If its not an event, only insert new items that are at least reactionTime in the future
					if(x.type == BeatmapDataItem.BeatmapDataItemType.BeatmapObject && relativeSongTime < reactionTime)
						continue;

					// Was there already a new / unused node we can overwrite to save on allocations?
					if(prevNode != null) {
						prevNode.Value = x;
						// No :(
					} else {
						prevNode = readonlyBeatmapData.allBeatmapDataItems.AddLast(x);
					}
					prevNode = prevNode.Next;

					// Change the nodes time to be at the right time in the modified context
					var j = x;
					var bmot = relativeSongTime + currentAudioTime;

					// Sliders and arcs have an end time that we need to correct also
					if(x.type == BeatmapDataItem.BeatmapDataItemType.BeatmapObject && x is SliderData xSlider)
						SETTER_SliderData_tailTime(ref xSlider) = bmot + xSlider.tailTime - x.time;

					SETTER_BeatmapDataItem_time(ref j) = bmot;
				}

				// Keep all beatmap items past the end of what we inserted now for Memory / Allocation reasons, but move them baaaack
				for(; prevNode != null; prevNode = prevNode.Next) {
					var iv = prevNode.Value;
					SETTER_BeatmapDataItem_time(ref iv) = float.MaxValue;
				}
			}

			if(beatmapObjectSpawnController != null) {
				// Update to the new maps stuffs
				var njs = replacementDifficultyBeatmap.noteJumpMovementSpeed;
				if(njs == 0)
					njs = BeatmapDifficultyMethods.NoteJumpMovementSpeed(replacementDifficultyBeatmap.difficulty);

				// This updates the spawnAheadTime amonguser things
				UpdateBeatmapObjectSpawnControllerInitData(
					replacementDifficultyBeatmap.level.beatsPerMinute,
					njs,
					replacementDifficultyBeatmap.noteJumpStartBeatOffset,
					replacementBeatmapData.numberOfLines
				);
			}

			if(beatmapCallbacksController != null) {
				foreach(var x in beatmapObjectCallbackController_callbacksInTimes) {
					var v = x.Value;
					var aheadTime = v.aheadTime;
					// I hate that i actually have to do this. Writes the correct / new aheadTime to the callbacks for the object spawns
					if(x.Key == startBeatmapCallbackAheadTime) {
						var cbs = FIELD_CallbacksInTime_callbacks(ref v);
						aheadTime = beatmapObjectSpawnMovementData.spawnAheadTime;
						foreach(var cbl in cbs.Values) {
							cbl.ForEach(cb => { SETTER_BeatmapDataCallbackWrapper_aheadTime(ref cb) = aheadTime; });
						}
					}

					while(v.lastProcessedNode.Next != null && v.lastProcessedNode.Next.Value.time < currentAudioTime)
						v.lastProcessedNode = v.lastProcessedNode.Next;
				}
			}

			// Allow stuff to spawn again before music switches (Disabled previously in NoSpawnZone())
			SetSpawningEnabled(true);


			var timePre = audioTimeWrapper.songTime;
			// Wait before swapping in the new audio until the new notes are a bit closer. Feels better
			yield return new WaitUntil(() => !audioTimeWrapper.isValid || audioTimeWrapper.songTime - timePre >= Math.Min(reactionTime * 0.5f, startTime));

			if(audioTimeWrapper == null)
				yield break;

			// New audio
			// This is where I would go ahead and fiddle with the AudioTimeSync controller and swap out the audio clip etc
			// But that would be a MASSIVE pain, so why dont we just create our own audioclip and sync that to the normal sync controller? :)
			customAudioSource.SetClip(replacementDifficultyBeatmap.level.beatmapLevelData.audioClip);
			customAudioSource.Play(startTime + (audioTimeWrapper.songTime - timePre));

			HeckOffCutSoundsCrash.enablePatch = false;

			if(Config.Instance.transition_gracePeriod > 0) {
				//TODO: Not sure if we need jump or move duration here, need to test
				yield return new WaitUntil(() => audioTimeWrapper == null || audioTimeWrapper.songTime > timePre + reactionTime);

				if(audioTimeWrapper == null)
					yield break;

				if(gameEnergyCounter != null)
					SETTER_GameEnergyCounter_noFail.Invoke(gameEnergyCounter, new object[] { true });

				yield return new WaitForSeconds(Config.Instance.transition_gracePeriod);

				if(audioTimeWrapper == null)
					yield break;

				if(gameEnergyCounter != null)
					SETTER_GameEnergyCounter_noFail.Invoke(gameEnergyCounter, new object[] { false });
			}
		}

		public void Dispose() {
			HeckOffCutSoundsCrash.enablePatch = false;
		}
	}
}
