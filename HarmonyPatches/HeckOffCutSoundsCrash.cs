using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SiraUtil.Affinity;

namespace Shaffuru.HarmonyPatches {
	class HeckOffCutSoundsCrash : IAffinity {
		public static bool enablePatch = false;

		[AffinityPatch(typeof(NoteCutSoundEffectManager), nameof(NoteCutSoundEffectManager.HandleNoteWasSpawned))]
		[AffinityPatch(typeof(NoteCutSoundEffectManager), nameof(NoteCutSoundEffectManager.HandleNoteWasCut))]
		[AffinityPrefix]
		bool Prefix() => !enablePatch;
	}
}
