using Shaffuru.GameLogic;
using Shaffuru.MenuLogic;
using Zenject;

namespace Shaffuru.Installers {
	class GameInstaller : MonoInstaller {
		public override void InstallBindings() {
			var setupData = Container.Resolve<GameplayCoreSceneSetupData>();

			Plugin.isShaffuruActive = false;
			if(setupData.difficultyBeatmap.level.levelID != Anlasser.LevelId)
				return;

			Plugin.isShaffuruActive = true;

			Container.BindInterfacesAndSelfTo<BeatmapLoader>().AsSingle();
			Container.BindInterfacesAndSelfTo<BeatmapSwitcher>().AsSingle();
			Container.BindInterfacesAndSelfTo<QueueProcessor>().AsSingle();

			Container.BindInterfacesAndSelfTo<IntroPlayer>().AsSingle().NonLazy();
		}
	}
}
