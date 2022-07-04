using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Shaffuru.HarmonyPatches {
	[HarmonyPatch(typeof(NoteCutSoundEffectManager))]
	static class HeckOffCutSoundsCrash {
		public static bool enablePatch = false;

		[HarmonyPatch(nameof(NoteCutSoundEffectManager.HandleNoteWasSpawned))]
		[HarmonyPatch(nameof(NoteCutSoundEffectManager.HandleNoteWasCut))]
		static bool Prefix() => !enablePatch;
	}
}
