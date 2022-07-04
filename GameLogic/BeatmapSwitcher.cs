using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Shaffuru.HarmonyPatches;
using UnityEngine;

namespace Shaffuru.GameLogic {
	[HarmonyPatch(typeof(BasicBeatmapObjectManager), nameof(BasicBeatmapObjectManager.Init))]
	static class BeatmapObjectsPools {
		static Action<float>[] activeItemPools;

		static void Postfix(
			MemoryPoolContainer<GameNoteController> ____basicGameNotePoolContainer,
			MemoryPoolContainer<GameNoteController> ____burstSliderHeadGameNotePoolContainer,
			MemoryPoolContainer<BurstSliderGameNoteController> ____burstSliderGameNotePoolContainer,
			MemoryPoolContainer<BurstSliderGameNoteController> ____burstSliderFillPoolContainer,
			MemoryPoolContainer<BombNoteController> ____bombNotePoolContainer,
			MemoryPoolContainer<ObstacleController> ____obstaclePoolContainer,
			Dictionary<SliderController.LengthType, MemoryPoolContainer<SliderController>> ____sliderNotePoolContainersDictionary
		) {
			activeItemPools = new Action<float>[] {
				(a) => ____basicGameNotePoolContainer.activeItems.ForEach(x => x.Dissolve(a)),
				(a) => ____burstSliderHeadGameNotePoolContainer.activeItems.ForEach(x => x.Dissolve(a)),
				(a) => ____burstSliderGameNotePoolContainer.activeItems.ForEach(x => x.Dissolve(a)),
				(a) => ____burstSliderFillPoolContainer.activeItems.ForEach(x => x.Dissolve(a)),
				(a) => ____bombNotePoolContainer.activeItems.ForEach(x => x.Dissolve(a)),
				(a) => ____obstaclePoolContainer.activeItems.ForEach(x => x.Dissolve(a)),
				(a) => ____sliderNotePoolContainersDictionary.Values.Do(x => x.activeItems.ForEach(x => x.Dissolve(a)))
			};
		}

		public static void DissolveAll(float dissolveTime) {
			foreach(var x in activeItemPools)
				x(dissolveTime);
		}
	}

	class BeatmapSwitcher : IDisposable {
		static readonly FieldInfo FIELD_BeatmapObjectSpawnController_beatmapObjectSpawnMovementData = AccessTools.Field(typeof(BeatmapObjectSpawnController), "_beatmapObjectSpawnMovementData");

		static readonly FieldInfo FIELD_BeatmapObjectCallbackController_callbacksInTimes = AccessTools.Field(typeof(BeatmapCallbacksController), "_callbacksInTimes");

		static readonly FieldInfo FIELD_AudioTimeSyncController_audioLatency = AccessTools.Field(typeof(AudioTimeSyncController), "_audioLatency");
		static readonly FieldInfo FIELD_AudioTimeSyncController_audioSource = AccessTools.Field(typeof(AudioTimeSyncController), "_audioSource");

		static readonly FieldInfo FIELD_GameSongController_failAudioPitchGainEffect = AccessTools.Field(typeof(GameSongController), "_failAudioPitchGainEffect");
		static readonly FieldInfo FIELD_CallbacksInTime_callbacks = AccessTools.Field(typeof(CallbacksInTime), "_callbacks");


		static readonly MethodInfo SETTER_GameEnergyCounter_noFail = AccessTools.PropertySetter(typeof(GameEnergyCounter), nameof(GameEnergyCounter.noFail));

		static readonly IPA.Utilities.FieldAccessor<BeatmapDataItem, float>.Accessor SETTER_BeatmapDataItem_time = IPA.Utilities.FieldAccessor<BeatmapDataItem, float>.GetAccessor($"<{nameof(BeatmapDataItem.time)}>k__BackingField");
		static readonly IPA.Utilities.FieldAccessor<SliderData, float>.Accessor SETTER_SliderData_tailTime = IPA.Utilities.FieldAccessor<SliderData, float>.GetAccessor($"<{nameof(SliderData.tailTime)}>k__BackingField");

		static readonly IPA.Utilities.FieldAccessor<BeatmapDataCallbackWrapper, float>.Accessor SETTER_BeatmapDataCallbackWrapper_aheadTime = IPA.Utilities.FieldAccessor<BeatmapDataCallbackWrapper, float>.GetAccessor("aheadTime");

		readonly GameplayCoreSceneSetupData _sceneSetupData;

		readonly BeatmapObjectSpawnController.InitData BeatmapObjectSpawnController_InitData;
		readonly float startBeatmapCallbackAheadTime;
		readonly IJumpOffsetYProvider jumpOffsetYProvider;

		readonly IReadonlyBeatmapData readonlyBeatmapData;

		readonly BeatmapCallbacksController beatmapCallbacksController;
		readonly IReadOnlyDictionary<float, CallbacksInTime> beatmapObjectCallbackController_callbacksInTimes;
		readonly AudioTimeSyncController audioTimeSyncController;
		readonly GameEnergyCounter gameEnergyCounter;

		readonly BeatmapObjectSpawnMovementData beatmapObjectSpawnMovementData;

		readonly RamCleaner ramCleaner = new RamCleaner();
		internal CustomSyncedAudioSource customAudioSource { get; private set; }

		public BeatmapSwitcher(
			GameplayCoreSceneSetupData _sceneSetupData,
			BeatmapObjectSpawnController.InitData BeatmapObjectSpawnController_InitData,
			IJumpOffsetYProvider jumpOffsetYProvider,
			IReadonlyBeatmapData readonlyBeatmapData,
			BeatmapObjectSpawnController beatmapObjectSpawnController,
			BeatmapCallbacksController beatmapCallbacksController,
			AudioTimeSyncController audioTimeSyncController,
			GameEnergyCounter gameEnergyCounter,
			GameSongController gameSongController
		) {
			this._sceneSetupData = _sceneSetupData;
			this.BeatmapObjectSpawnController_InitData = BeatmapObjectSpawnController_InitData;
			this.jumpOffsetYProvider = jumpOffsetYProvider;
			this.readonlyBeatmapData = readonlyBeatmapData;
			this.beatmapCallbacksController = beatmapCallbacksController;
			this.audioTimeSyncController = audioTimeSyncController;
			this.gameEnergyCounter = gameEnergyCounter;

			var latency = (FloatSO)FIELD_AudioTimeSyncController_audioLatency.GetValue(audioTimeSyncController);

			customAudioSource = new CustomSyncedAudioSource(audioTimeSyncController, latency.value);
			// The audio effect when you play, need to apply that onto our custom audio source
			((AudioPitchGainEffect)FIELD_GameSongController_failAudioPitchGainEffect.GetValue(gameSongController)).SetAudioSource(customAudioSource.source);

			beatmapObjectCallbackController_callbacksInTimes = (Dictionary<float, CallbacksInTime>)FIELD_BeatmapObjectCallbackController_callbacksInTimes.GetValue(beatmapCallbacksController);

			beatmapObjectSpawnMovementData = (BeatmapObjectSpawnMovementData)FIELD_BeatmapObjectSpawnController_beatmapObjectSpawnMovementData.GetValue(beatmapObjectSpawnController);
			startBeatmapCallbackAheadTime = beatmapObjectSpawnMovementData.spawnAheadTime;

			// We dont need that to play
			((AudioSource)FIELD_AudioTimeSyncController_audioSource.GetValue(audioTimeSyncController)).mute = true;
			Plugin.Log.Debug("BeatmapSwitcher Created");
		}

		void UpdateBeatmapObjectSpawnControllerInitData(float bpm, float njs, float mapOffset) {
			BeatmapObjectSpawnControllerHelpers.GetNoteJumpValues(_sceneSetupData.playerSpecificSettings, mapOffset, out var noteJumpValueType, out var noteJumpValue);
			beatmapObjectSpawnMovementData.Init(
				// If this changes the game would probably explode
				BeatmapObjectSpawnController_InitData.noteLinesCount,
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

		public void SwitchToDifferentBeatmap(IDifficultyBeatmap difficultyBeatmap, IReadonlyBeatmapData replacementBeatmapData, float startTime, float insertBeatmapUntilTime = 0) {
			audioTimeSyncController.StartCoroutine(Switcher(difficultyBeatmap, replacementBeatmapData, startTime, insertBeatmapUntilTime));
		}

		IEnumerator Switcher(IDifficultyBeatmap replacementDifficultyBeatmap, IReadonlyBeatmapData replacementBeatmapData, float startTime, float insertBeatmapUntilTime = 0) {
			// Get the "spawn ahead" time the current song uses - These are notes which are spawned / moving in
			// before the specific song location has been reached
			//var oldJumpDuration = beatmapObjectSpawnController.jumpDuration;
			//var oldMoveDuration = beatmapObjectSpawnController.moveDuration;

			//Console.WriteLine("{0} {1} {2}", oldJumpDuration, oldMoveDuration, beatmapObjectSpawnMovementData.spawnAheadTime - oldJumpDuration - oldMoveDuration);

			var audioSource = (AudioSource)FIELD_AudioTimeSyncController_audioSource.GetValue(audioTimeSyncController);

			var reactionTime = Config.Instance.transition_reactionTime;
			var dissolveTime = reactionTime * 0.6f;

			// We are switching to the new map... Dissolve everything thats active right now
			BeatmapObjectsPools.DissolveAll(dissolveTime);

			HeckOffCutSoundsCrash.enablePatch = true;

			if(!ramCleaner.TrySkip() && audioTimeSyncController.songLength - audioTimeSyncController.songTime >= 30f) {
				yield return new WaitForSecondsRealtime(dissolveTime * 0.8f);
				audioSource.Pause();

				customAudioSource.SetAudio(replacementDifficultyBeatmap.level.beatmapLevelData.audioClip);

				yield return new WaitForSecondsRealtime(dissolveTime * 0.2f);
				yield return ramCleaner.ClearRam();

				// Make sure we have had at least 20 frames, not just half a second
				var s = Time.time;
				for(var i = 0; i < 20; i++)
					yield return null;
				yield return new WaitUntil(() => audioTimeSyncController == null || Time.time - s >= 0.5f);
				audioSource.UnPause();
			} else {
				// Force this to execute after Behaviour Update()'s so the TimeSyncController is up-to-date
				yield return null;
			}

			if(audioTimeSyncController == null)
				yield break;

			var currentAudioTime = audioTimeSyncController.songTime;

			LinkedListNode<BeatmapDataItem> prevNode = null;

			// Find the newest node processed by any callback (biggest aheadTime)
			foreach(var x in beatmapObjectCallbackController_callbacksInTimes) {
				if(prevNode == null || prevNode.Value.time < x.Value.lastProcessedNode.Value.time)
					prevNode = x.Value.lastProcessedNode.Next;
			}

			foreach(var x in replacementBeatmapData.allBeatmapDataItems) {
				var relativeSongTime = x.time - startTime;

				// If we are past the time which we want to add / replace in, break
				if(insertBeatmapUntilTime > 0 && relativeSongTime > insertBeatmapUntilTime + 1)
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

			// Update to the new maps stuffs
			var njs = replacementDifficultyBeatmap.noteJumpMovementSpeed;
			if(njs == 0)
				njs = BeatmapDifficultyMethods.NoteJumpMovementSpeed(replacementDifficultyBeatmap.difficulty);

			// This updates the spawnAheadTime amonguser things
			UpdateBeatmapObjectSpawnControllerInitData(replacementDifficultyBeatmap.level.beatsPerMinute, njs, replacementDifficultyBeatmap.noteJumpStartBeatOffset);

			foreach(var x in beatmapObjectCallbackController_callbacksInTimes) {
				var v = x.Value;
				var aheadTime = v.aheadTime;
				// I hate that i actually have to do this. Writes the correct / new aheadTime to the callbacks for the object spawns
				if(x.Key == startBeatmapCallbackAheadTime) {
					var cbs = (Dictionary<Type, List<BeatmapDataCallbackWrapper>>)FIELD_CallbacksInTime_callbacks.GetValue(v);
					aheadTime = beatmapObjectSpawnMovementData.spawnAheadTime;
					foreach(var cbl in cbs.Values) {
						cbl.ForEach(cb => { SETTER_BeatmapDataCallbackWrapper_aheadTime(ref cb) = aheadTime; });
					}
				}

				while(v.lastProcessedNode.Next != null && v.lastProcessedNode.Next.Value.time < currentAudioTime)
					v.lastProcessedNode = v.lastProcessedNode.Next;
			}

			// Dissolve again to be sure because Beat Saber™
			BeatmapObjectsPools.DissolveAll(dissolveTime);


			var timePre = audioTimeSyncController.songTime;
			// Wait before swapping in the new audio until the new notes are a bit closer. Feels better
			yield return new WaitUntil(() => audioTimeSyncController == null || audioTimeSyncController.songTime - timePre >= Math.Min(reactionTime * 0.5f, startTime));

			if(audioTimeSyncController == null)
				yield break;

			// New audio
			// This is where I would go ahead and fiddle with the AudioTimeSync controller and swap out the audio clip etc
			// But that would be a MASSIVE pain, so why dont we just create our own audioclip and sync that to the normal sync controller? :)
			customAudioSource.SetAudio(replacementDifficultyBeatmap.level.beatmapLevelData.audioClip);
			customAudioSource.Play(startTime + (audioTimeSyncController.songTime - timePre));

			HeckOffCutSoundsCrash.enablePatch = false;

			if(Config.Instance.transition_gracePeriod > 0) {
				//TODO: Not sure if we need jump or move duration here, need to test
				yield return new WaitUntil(() => audioTimeSyncController == null || audioTimeSyncController.songTime > timePre + reactionTime);

				if(audioTimeSyncController == null)
					yield break;

				SETTER_GameEnergyCounter_noFail.Invoke(gameEnergyCounter, new object[] { true });

				yield return new WaitForSeconds(Config.Instance.transition_gracePeriod);

				if(audioTimeSyncController == null)
					yield break;

				SETTER_GameEnergyCounter_noFail.Invoke(gameEnergyCounter, new object[] { false });
			}
		}

		public void Dispose() {
			HeckOffCutSoundsCrash.enablePatch = false;
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

		public void SetAudio(AudioClip clip) {
			if(source.clip == clip)
				return;

			source.Stop();
			//source.clip?.UnloadAudioData();
			source.time = 0f;
			source.clip = clip;
		}

		public void Play(float songStart = 0) {
			if(source.clip != null) {
				source.time = songStart + audioLatency;
				source.Play();
				//Console.WriteLine("{0} {1}", source.isPlaying, source.clip.length);
			}
		}
	}
}
