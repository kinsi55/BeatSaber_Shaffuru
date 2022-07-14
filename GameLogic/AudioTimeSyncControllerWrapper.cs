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
	class AudioTimeSyncControllerWrapper : ITickable {
		static readonly FieldInfo FIELD_AudioTimeSyncController_audioLatency = AccessTools.Field(typeof(AudioTimeSyncController), "_audioLatency");
		static readonly FieldInfo FIELD_AudioTimeSyncController_audioSource = AccessTools.Field(typeof(AudioTimeSyncController), "_audioSource");

		public readonly AudioTimeSyncController audioTimeSyncController;
		public AudioTimeSyncControllerWrapper(AudioTimeSyncController audioTimeSyncController) {
			this.audioTimeSyncController = audioTimeSyncController;
		}

		float tbase = -1;
		float time = 0;

		public FloatSO audioLatency => (FloatSO)FIELD_AudioTimeSyncController_audioLatency.GetValue(audioTimeSyncController);
		public AudioSource audioSource => (AudioSource)FIELD_AudioTimeSyncController_audioSource.GetValue(audioTimeSyncController);
		public float songTime => time;
		public float songLength => audioTimeSyncController.songLength;


		public void Tick() {
			if(!audioTimeSyncController.isActiveAndEnabled) {
				if(tbase <= 0)
					tbase = Time.realtimeSinceStartup - audioTimeSyncController.songTime;

				time = Time.realtimeSinceStartup - tbase;
			} else {
				tbase = -1;
				time = audioTimeSyncController.songTime;
			}
		}
	}
}
