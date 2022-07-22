using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;
using Zenject;

namespace Shaffuru.GameLogic {
	class CustomAudioSource {
		static readonly FieldInfo FIELD_GameSongController_failAudioPitchGainEffect 
			= AccessTools.Field(typeof(GameSongController), "_failAudioPitchGainEffect");

		readonly AudioMixerGroup mixer;
		readonly float defaultPitch = 0;

		readonly AudioTimeWrapper audioTimeWrapper;

		readonly AudioSource source;
		readonly float audioLatency = 0;

		public CustomAudioSource(
			GameplayCoreSceneSetupData gameplayCoreSceneSetupData,
			AudioTimeWrapper audioTimeWrapper
		) {
			// The fact that I have to do this is complete garbage
			if(mixer == null) {
				mixer = UnityEngine.Object.FindObjectsOfType<AudioSource>()
					.Where(x => x.outputAudioMixerGroup?.name == "Music")
					.FirstOrDefault()
					?.outputAudioMixerGroup;
			}

			this.audioTimeWrapper = audioTimeWrapper;

			audioLatency = gameplayCoreSceneSetupData.mainSettingsModel.audioLatency;

			source = new GameObject("CAS", typeof(AudioSource)).GetComponent<AudioSource>();

			source.outputAudioMixerGroup = mixer;

			// It could have been so easy if they just deduplicated this code...
			defaultPitch = gameplayCoreSceneSetupData.gameplayModifiers.songSpeedMul;
		}

		public void SetClip(AudioClip clip) {
			if(source.clip == clip)
				return;

			source.Stop();
			source.time = 0f;
			source.clip = clip;
		}

		float lastSetTime = 0;
		public void Play(float songStart = 0) {
			if(source.clip != null) {
				source.time = songStart + audioLatency;
				lastSetTime = source.time - audioTimeWrapper.songTime;

				source.Play();
			}
		}

		// Seems like this isnt actually used in song because the volume is set with the audiomixergroup
		const float defaultVolume = 1;

		public void YoinkFailEffect(GameSongController gameSongController) {
			source.pitch = defaultPitch;
			source.volume = defaultVolume;

			((AudioPitchGainEffect)FIELD_GameSongController_failAudioPitchGainEffect.GetValue(gameSongController))?.SetAudioSource(source);
		}

		public void ResetFailEffect() {
			source.pitch = defaultPitch;
			source.volume = defaultVolume;

			Play(lastSetTime + audioTimeWrapper.songTime);
		}
	}
}
