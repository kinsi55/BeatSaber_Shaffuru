using Shaffuru.AppLogic;
using Zenject;

namespace Shaffuru.Installers {
	class AppInstaller : MonoInstaller {
		public override void InstallBindings() {
			Container.BindInterfacesAndSelfTo<MapPool>().AsSingle().NonLazy();
			Container.BindInterfacesAndSelfTo<SongQueueManager>().AsSingle().NonLazy();

			if(IPA.Loader.PluginManager.GetPluginFromId("ChatCore") == null)
				return;

			Container.BindInterfacesAndSelfTo<RequestManager>().AsSingle().NonLazy();
		}
	}
}
