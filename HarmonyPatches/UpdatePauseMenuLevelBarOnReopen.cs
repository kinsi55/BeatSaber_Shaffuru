using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace Shaffuru.HarmonyPatches {
	[HarmonyPatch(typeof(PauseMenuManager), nameof(PauseMenuManager.ShowMenu))]
	static class UpdatePauseMenuLevelBarOnReopen {
		static void Postfix(PauseMenuManager __instance) {
			if(!Plugin.isShaffuruActive)
				return;

			__instance.Start();
		}
	}
}
