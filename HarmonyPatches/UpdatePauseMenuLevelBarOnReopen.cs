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
