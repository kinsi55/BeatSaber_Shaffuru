using Shaffuru.AppLogic;
using Shaffuru.GameLogic;
using Shaffuru.MenuLogic;
using Zenject;

namespace Shaffuru.Installers {
	class GameInstaller : MonoInstaller {
		public override void InstallBindings() {
			var setupData = Container.Resolve<GameplayCoreSceneSetupData>();

			Plugin.isShaffuruActive = false;
			if(setupData.difficultyBeatmap.level.levelID.StartsWith(Anlasser.LevelIdPrefix, System.StringComparison.OrdinalIgnoreCase))
				return;

			TheStuff();

			Container.BindInterfacesAndSelfTo<IntroPlayer>().AsSingle().NonLazy();
		}

		public void TheStuff() {
			Plugin.isShaffuruActive = true;

			Container.BindInterfacesAndSelfTo<BeatmapLoader>().AsSingle();
			Container.BindInterfacesAndSelfTo<BeatmapSwitcher>().AsSingle();
			Container.BindInterfacesAndSelfTo<QueueProcessor>().AsSingle().NonLazy();

			var mapPool = Container.Resolve<MapPool>();

			if(mapPool.currentFilterConfig.allowME && IPA.Loader.PluginManager.GetPluginFromId("MappingExtensions") != null)
				EnableME();
		}

		static void EnableME() {
			MappingExtensions.Plugin.ForceActivateForSong();
		}
	}
}
