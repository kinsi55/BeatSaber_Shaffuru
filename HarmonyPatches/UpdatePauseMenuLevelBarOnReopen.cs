using HarmonyLib;
using SiraUtil.Affinity;

namespace Shaffuru.HarmonyPatches {
	class UpdatePauseMenuLevelBarOnReopen : IAffinity {
		[AffinityPatch(typeof(PauseMenuManager), nameof(PauseMenuManager.ShowMenu))]
		[AffinityPostfix]
		void Postfix(PauseMenuManager __instance) => __instance.Start();
	}
}
