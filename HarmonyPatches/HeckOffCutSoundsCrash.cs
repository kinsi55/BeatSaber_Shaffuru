using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Shaffuru.HarmonyPatches {
	[HarmonyPatch]
	static class HeckOffCutSoundsCrash {
		public static bool enablePatch = false;

		static IEnumerable<MethodBase> TargetMethods() {
			yield return AccessTools.Method(typeof(NoteCutSoundEffectManager), nameof(NoteCutSoundEffectManager.HandleNoteWasSpawned));
			yield return AccessTools.Method(typeof(NoteCutSoundEffectManager), nameof(NoteCutSoundEffectManager.HandleNoteWasCut));
		}

		static bool Prefix() => !enablePatch;
	}
}
