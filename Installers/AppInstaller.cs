using Shaffuru.AppLogic;
using Zenject;

namespace Shaffuru.Installers {
	class AppInstaller : MonoInstaller {
		public override void InstallBindings() {
			Container.Bind<PlayedSongList>().FromInstance(new PlayedSongList()).AsSingle();

			Container.BindInterfacesAndSelfTo<MapPool>().AsSingle().NonLazy();
			Container.BindInterfacesAndSelfTo<SongQueueManager>().AsSingle().NonLazy();

			if(IPA.Loader.PluginManager.GetPluginFromId("CatCore") == null)
				return;

			Container.BindInterfacesAndSelfTo<RequestManager>().AsSingle().NonLazy();
		}
	}
}
