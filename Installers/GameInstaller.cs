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

			Container.BindInterfacesAndSelfTo<SongQueueManager>().AsSingle();

			CoreInstaller();

			Container.BindInterfacesTo<UnifiedATSC>().AsSingle().NonLazy();

			IngameInstaller();

			Container.BindInterfacesTo<BeatmapSwitchSubscriber>().AsSingle().WithArguments(0).NonLazy();

			Container.BindInterfacesTo<UpdatePauseMenuLevelBarOnReopen>().AsSingle();
			Container.BindInterfacesTo<IntroPlayer>().AsSingle().NonLazy();
		}

		public void IngameInstaller() {
			Container.BindInterfacesTo<BeatmapObjectDissolver>().AsSingle().NonLazy();
			Container.BindInterfacesAndSelfTo<BeatmapSwitcher>().AsSingle();
		}

		public void CoreInstaller() {
			Container.BindInterfacesAndSelfTo<AudioTimeWrapper>().AsSingle();
			Container.Bind<CustomAudioSource>().AsSingle();
			Container.BindInterfacesTo<HeckOffCutSoundsCrash>().AsSingle();


			Container.BindInterfacesAndSelfTo<BeatmapLoader>().AsSingle();
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
