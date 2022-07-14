using Shaffuru.AppLogic;
using Shaffuru.GameLogic;
using Shaffuru.HarmonyPatches;
using Shaffuru.MenuLogic;
using Zenject;

namespace Shaffuru.Installers {
	class GameInstaller : MonoInstaller {
		public override void InstallBindings() {
			var setupData = Container.Resolve<GameplayCoreSceneSetupData>();

			if(!setupData.difficultyBeatmap.level.levelID.StartsWith(Anlasser.LevelIdPrefix, System.StringComparison.OrdinalIgnoreCase))
				return;

			TheStuff();

			Container.BindInterfacesTo<UpdatePauseMenuLevelBarOnReopen>().AsSingle();
			Container.BindInterfacesTo<IntroPlayer>().AsSingle().NonLazy();
		}

		public void TheStuff() {
			Container.BindInterfacesTo<HeckOffCutSoundsCrash>().AsSingle();

			Container.BindInterfacesAndSelfTo<AudioTimeSyncControllerWrapper>().AsSingle();

			Container.BindInterfacesAndSelfTo<BeatmapLoader>().AsSingle();
			Container.BindInterfacesAndSelfTo<BeatmapSwitcher>().AsSingle();
			Container.BindInterfacesAndSelfTo<QueueProcessor>().AsSingle().NonLazy();
			Container.BindInterfacesTo<BeatmapObjectDissolver>().AsSingle().NonLazy();

			var mapPool = Container.Resolve<MapPool>();

			if(mapPool.currentFilterConfig.allowME && IPA.Loader.PluginManager.GetPluginFromId("MappingExtensions") != null)
				EnableME();
		}

		static void EnableME() {
			MappingExtensions.Plugin.ForceActivateForSong();
		}
	}
}
