using HarmonyLib;
using Shaffuru.HarmonyPatches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Zenject;

namespace Shaffuru.GameLogic {
	public class BeatmapSwitcher : IInitializable, IDisposable {
		public static BeatmapSwitcher instance;

		static readonly FieldInfo FIELD_BasicBeatmapObjectManager_basicGameNotePoolContainer = AccessTools.Field(typeof(BasicBeatmapObjectManager), "_basicGameNotePoolContainer");
		static readonly FieldInfo FIELD_BasicBeatmapObjectManager_burstSliderHeadGameNotePoolContainer = AccessTools.Field(typeof(BasicBeatmapObjectManager), "_burstSliderHeadGameNotePoolContainer");
		static readonly FieldInfo FIELD_BasicBeatmapObjectManager_burstSliderGameNotePoolContainer = AccessTools.Field(typeof(BasicBeatmapObjectManager), "_burstSliderGameNotePoolContainer");
		static readonly FieldInfo FIELD_BasicBeatmapObjectManager_burstSliderFillPoolContainer = AccessTools.Field(typeof(BasicBeatmapObjectManager), "_burstSliderFillPoolContainer");
		static readonly FieldInfo FIELD_BasicBeatmapObjectManager_bombNotePoolContainer = AccessTools.Field(typeof(BasicBeatmapObjectManager), "_bombNotePoolContainer");
		static readonly FieldInfo FIELD_BasicBeatmapObjectManager_obstaclePoolContainer = AccessTools.Field(typeof(BasicBeatmapObjectManager), "_obstaclePoolContainer");

		static readonly FieldInfo FIELD_BeatmapObjectSpawnController_beatmapObjectSpawnMovementData = AccessTools.Field(typeof(BeatmapObjectSpawnController), "_beatmapObjectSpawnMovementData");

		static readonly FieldInfo FIELD_BeatmapObjectCallbackController_callbacksInTimes = AccessTools.Field(typeof(BeatmapCallbacksController), "_callbacksInTimes");
		//static readonly FieldInfo FIELD_BeatmapObjectCallbackController_nextEventIndex = AccessTools.Field(typeof(BeatmapObjectCallbackController), "_nextEventIndex");

		static readonly FieldInfo FIELD_AudioTimeSyncController_audioLatency = AccessTools.Field(typeof(AudioTimeSyncController), "_audioLatency");
		static readonly FieldInfo FIELD_AudioTimeSyncController_audioSource = AccessTools.Field(typeof(AudioTimeSyncController), "_audioSource");

		static readonly FieldInfo FIELD_GameSongController_failAudioPitchGainEffect = AccessTools.Field(typeof(GameSongController), "_failAudioPitchGainEffect");


		static readonly MethodInfo SETTER_GameEnergyCounter_noFail = AccessTools.PropertySetter(typeof(GameEnergyCounter), nameof(GameEnergyCounter.noFail));

		static Action<BeatmapDataItem, float> FIELD_BeatmapDataItem_time_SETTER;


		readonly GameplayCoreSceneSetupData _sceneSetupData;

		readonly BeatmapObjectSpawnController.InitData BeatmapObjectSpawnController_InitData;
		readonly IJumpOffsetYProvider jumpOffsetYProvider;

		readonly IReadonlyBeatmapData readonlyBeatmapData;

		readonly BasicBeatmapObjectManager basicBeatmapObjectManager;

		readonly BeatmapObjectSpawnController beatmapObjectSpawnController;
		readonly BeatmapCallbacksController beatmapCallbacksController;
		readonly Dictionary<float, CallbacksInTime> beatmapObjectCallbackController_callbacksInTimes;
		readonly AudioTimeSyncController audioTimeSyncController;
		readonly GameEnergyCounter gameEnergyCounter;
		readonly GameSongController gameSongController;

		readonly RamCleaner ramCleaner = new RamCleaner();

		public BeatmapSwitcher(
			GameplayCoreSceneSetupData _sceneSetupData,
			BeatmapObjectSpawnController.InitData BeatmapObjectSpawnController_InitData,
			IJumpOffsetYProvider jumpOffsetYProvider,
			IReadonlyBeatmapData readonlyBeatmapData,
			BasicBeatmapObjectManager basicBeatmapObjectManager,
			BeatmapObjectSpawnController beatmapObjectSpawnController,
			BeatmapCallbacksController beatmapCallbacksController,
			AudioTimeSyncController audioTimeSyncController,
			GameEnergyCounter gameEnergyCounter,
			GameSongController gameSongController
		) {
			instance = this;

			this._sceneSetupData = _sceneSetupData;
			this.BeatmapObjectSpawnController_InitData = BeatmapObjectSpawnController_InitData;
			this.jumpOffsetYProvider = jumpOffsetYProvider;
			this.readonlyBeatmapData = readonlyBeatmapData;
			this.basicBeatmapObjectManager = basicBeatmapObjectManager;
			this.beatmapObjectSpawnController = beatmapObjectSpawnController;
			this.beatmapCallbacksController = beatmapCallbacksController;
			this.audioTimeSyncController = audioTimeSyncController;
			this.gameEnergyCounter = gameEnergyCounter;
			this.gameSongController = gameSongController;

			beatmapObjectCallbackController_callbacksInTimes = (Dictionary<float, CallbacksInTime>)FIELD_BeatmapObjectCallbackController_callbacksInTimes.GetValue(beatmapCallbacksController);

			if(FIELD_BeatmapDataItem_time_SETTER == null) {
				var dm = new DynamicMethod("BeatmapDataItem_time_SETTER", null, new[] { typeof(BeatmapDataItem), typeof(float) }, true);

				var il = dm.GetILGenerator(5);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Stfld, AccessTools.Field(typeof(BeatmapDataItem), $"<{nameof(BeatmapDataItem.time)}>k__BackingField"));
				il.Emit(OpCodes.Ret);

				FIELD_BeatmapDataItem_time_SETTER = (Action<BeatmapDataItem, float>)dm.CreateDelegate(typeof(Action<BeatmapDataItem, float>));
			}
		}

		internal CustomSyncedAudioSource customAudioSource { get; private set; }

		Action<float>[] beatmapObjectDissolvers;

		//List<BeatmapObjectCallbackData> BeatmapObjectCallbackData_nextObjectIndexInLine;

		BeatmapObjectSpawnMovementData beatmapObjectSpawnMovementData;

		public void Initialize() {
			beatmapObjectDissolvers = new Action<float>[] {
				(d) => {
					foreach(var x in ((MemoryPoolContainer<GameNoteController>)FIELD_BasicBeatmapObjectManager_basicGameNotePoolContainer.GetValue(basicBeatmapObjectManager)).activeItems)
						x.Dissolve(d);
				}, (d) => {
					foreach(var x in ((MemoryPoolContainer<GameNoteController>)FIELD_BasicBeatmapObjectManager_burstSliderHeadGameNotePoolContainer.GetValue(basicBeatmapObjectManager)).activeItems)
						x.Dissolve(d);
				}, (d) => {
					foreach(var x in ((MemoryPoolContainer<BurstSliderGameNoteController>)FIELD_BasicBeatmapObjectManager_burstSliderGameNotePoolContainer.GetValue(basicBeatmapObjectManager)).activeItems)
						x.Dissolve(d);
				}, (d) => {
					foreach(var x in ((MemoryPoolContainer<BombNoteController>)FIELD_BasicBeatmapObjectManager_bombNotePoolContainer.GetValue(basicBeatmapObjectManager)).activeItems)
						x.Dissolve(d);
				}, (d) => {
					foreach(var x in ((MemoryPoolContainer<ObstacleController>)FIELD_BasicBeatmapObjectManager_obstaclePoolContainer.GetValue(basicBeatmapObjectManager)).activeItems)
						x.Dissolve(d);
				}
			};


			beatmapObjectSpawnMovementData = (BeatmapObjectSpawnMovementData)FIELD_BeatmapObjectSpawnController_beatmapObjectSpawnMovementData.GetValue(beatmapObjectSpawnController);

			var latency = (FloatSO)FIELD_AudioTimeSyncController_audioLatency.GetValue(audioTimeSyncController);

			customAudioSource = new CustomSyncedAudioSource(audioTimeSyncController, latency.value);
			// The audio effect when you play, need to apply that onto our custom audio source
			((AudioPitchGainEffect)FIELD_GameSongController_failAudioPitchGainEffect.GetValue(gameSongController)).SetAudioSource(customAudioSource.source);

			// We dont need that to play
			((AudioSource)FIELD_AudioTimeSyncController_audioSource.GetValue(audioTimeSyncController)).mute = true;
			Plugin.Log.Debug("BeatmapSwitcher loaded");
		}

		void UpdateBeatmapObjectSpawnControllerInitData(float bpm, float njs, float offset) {
			beatmapCallbacksController.TriggerBeatmapEvent(new BPMChangeBeatmapEventData(0, bpm));
			beatmapObjectSpawnMovementData.Init(
				// If this changes the game would probably explode
				BeatmapObjectSpawnController_InitData.noteLinesCount,
				njs,
				bpm,
				BeatmapObjectSpawnMovementData.NoteJumpValueType.BeatOffset,
				offset,
				jumpOffsetYProvider,
				// This (probably?) wont work with 360 - too bad!
				Vector3.right,
				Vector3.forward
			);
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
			var dissolveTime = reactionTime * 0.3f;

			void DissolveAll(float dissolveTime) {
				foreach(var x in beatmapObjectDissolvers)
					x(dissolveTime);
			}

			// We are switching to the new map... Dissolve everything thats active right now
			DissolveAll(dissolveTime);

			HeckOffCutSoundsCrash.enablePatch = true;

			if(!ramCleaner.TrySkip() && audioTimeSyncController.songLength - audioTimeSyncController.songTime >= 30f) {
				yield return new WaitForSecondsRealtime(dissolveTime * 0.8f);
				customAudioSource.SetAudio(null);
				audioSource.Pause();

				yield return new WaitForSecondsRealtime(dissolveTime * 0.2f);
				yield return ramCleaner.ClearRam();

				// Make sure we have had at least 20 frames, not just half a second
				var s = audioTimeSyncController.songTime;
				for(var i = 0; i < 20; i++)
					yield return null;
				yield return new WaitUntil(() => audioTimeSyncController == null || audioTimeSyncController.songTime - s >= 0.5f);
				audioSource.UnPause();
			} else {
				// Force this to execute after Behaviour Update()'s so the TimeSyncController is up-to-date
				yield return null;
			}

			if(audioTimeSyncController == null)
				yield break;

			// Yeet everything from the beatmap, starting from the index the map is being currently processed from
			// This is a MASSIVE amount of hack

			var currentAudioTime = audioTimeSyncController.songTime;


			//var x = readonlyBeatmapData.allBeatmapDataItems.Append();

			LinkedListNode<BeatmapDataItem> prevNode = null;

			foreach(var x in beatmapObjectCallbackController_callbacksInTimes) {
				if(prevNode == null || prevNode.Value.time < x.Value.lastProcessedNode.Value.time)
					prevNode = x.Value.lastProcessedNode;
			}

			var preadd = (prevNode?.Value.time ?? currentAudioTime) - currentAudioTime;

			foreach(var x in replacementBeatmapData.allBeatmapDataItems) {
				var t = x.time - startTime;

				if(insertBeatmapUntilTime > 0 && t > insertBeatmapUntilTime + 1)
					break;

				if(t < 0) {
					if(x.type != BeatmapDataItem.BeatmapDataItemType.BeatmapObject || !(x is ObstacleData woll))
						continue;

					var newLength = woll.duration - reactionTime + t;

					if(newLength < 0)
						continue;

					woll.UpdateDuration(newLength);

					t = 0f;
				}

				prevNode = prevNode.Next;
				if(prevNode != null) {
					prevNode.Value = x;
				} else {
					prevNode = readonlyBeatmapData.allBeatmapDataItems.AddLast(x);
				}

				FIELD_BeatmapDataItem_time_SETTER(x, t + currentAudioTime + reactionTime);
			}

			for(var i = prevNode.Next; i != null; i = i.Next)
				FIELD_BeatmapDataItem_time_SETTER(i.Value, float.MaxValue);

			// Update to the new maps stuffs
			var njs = replacementDifficultyBeatmap.noteJumpMovementSpeed;
			if(njs == 0)
				njs = BeatmapDifficultyMethods.NoteJumpMovementSpeed(replacementDifficultyBeatmap.difficulty);

			UpdateBeatmapObjectSpawnControllerInitData(replacementDifficultyBeatmap.level.beatsPerMinute, njs, replacementDifficultyBeatmap.noteJumpStartBeatOffset);

			foreach(var x in beatmapObjectCallbackController_callbacksInTimes.Values) {
				var b = x.aheadTime <= 1f ? 0 : beatmapObjectSpawnMovementData.spawnAheadTime - x.aheadTime;

				while(x.lastProcessedNode.Next != null && x.lastProcessedNode.Next.Value.time + b <= currentAudioTime)
					x.lastProcessedNode = x.lastProcessedNode.Next;
			}

			// Dissolve again to be sure because Beat Saber™
			DissolveAll(dissolveTime);


			var timePre = audioTimeSyncController.songTime;
			// Wait before swapping in the new audio until the new notes are a bit closer. Feels better
			yield return new WaitUntil(() => audioTimeSyncController == null || audioTimeSyncController.songTime - timePre >= Math.Min(reactionTime * 0.5f, startTime));

			if(audioTimeSyncController == null)
				yield break;

			// New audio
			// This is where I would go ahead and fiddle with the AudioTimeSync controller and swap out the audio clip etc
			// But that would be a MASSIVE pain, so why dont we just create our own audioclip and sync that to the normal sync controller? :)
			customAudioSource.SetAudio(replacementDifficultyBeatmap.level.beatmapLevelData.audioClip, startTime - reactionTime + (audioTimeSyncController.songTime - timePre));

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
