using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace Shaffuru.HarmonyPatches {
	[HarmonyPatch]
	static class HeckOffCutSoundsCrash {
		public static bool enablePatch = false;

		static IEnumerable<MethodBase> TargetMethods() {
			yield return AccessTools.Method(typeof(NoteCutSoundEffectManager), nameof(NoteCutSoundEffectManager.HandleNoteWasSpawned));
			yield return AccessTools.Method(typeof(NoteCutSoundEffectManager), nameof(NoteCutSoundEffectManager.HandleNoteWasCut));
		}

		static bool Prefix() => enablePatch;
	}
}
