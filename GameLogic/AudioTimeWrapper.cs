using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Zenject;

namespace Shaffuru.GameLogic {
	interface UnifiedAudioSource {
		public float songTime { get; }
		bool isValid { get; }

		public void Pause();
		public void Resume();
	}

	class UnifiedATSC : UnifiedAudioSource {
		static readonly FieldInfo FIELD_AudioTimeSyncController_audioSource = AccessTools.Field(typeof(AudioTimeSyncController), "_audioSource");

		AudioTimeSyncController audioTimeSyncController;
		AudioSource audioSource;

		public UnifiedATSC(AudioTimeSyncController audioTimeSyncController, AudioTimeWrapper audioTimeWrapper) {
			this.audioTimeSyncController = audioTimeSyncController;
			audioSource = (AudioSource)FIELD_AudioTimeSyncController_audioSource.GetValue(audioTimeSyncController);

			audioTimeWrapper.SetAudioSource(this);
		}

		public bool isValid => audioTimeSyncController != null;

		public float songTime => audioTimeSyncController.songTime;


		public void Pause() => audioSource.Pause();
		public void Resume() => audioSource.Play();
	}

	class AudioTimeWrapper {
		UnifiedAudioSource unifiedAudioSource;

		public AudioTimeWrapper(GameplayCoreSceneSetupData sceneSetupData) {
			songLength = sceneSetupData.difficultyBeatmap.level.beatmapLevelData.audioClip.length;
		}

		public void SetAudioSource(UnifiedAudioSource source) {
			unifiedAudioSource = source;
		}

		public bool isValid => unifiedAudioSource?.isValid == true;


		public readonly float songLength;
		public float songTime => unifiedAudioSource?.songTime ?? 0;
		public void Pause() => unifiedAudioSource?.Pause();
		public void Resume() => unifiedAudioSource?.Resume();
	}
}
