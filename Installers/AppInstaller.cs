using Shaffuru.AppLogic;
using SiraUtil.Zenject;
using System;
using Zenject;

namespace Shaffuru.Installers {
	class AppInstaller : MonoInstaller {
		public override void InstallBindings() {
			Container.Bind<PlayedSongList>().FromInstance(new PlayedSongList()).AsSingle();

			Container.BindInterfacesAndSelfTo<MapPool>().AsSingle().NonLazy();
			Container.BindInterfacesAndSelfTo<SongQueueManager>().AsSingle().NonLazy();

			Container.BindInstance(new UBinder<Plugin, Random>(new Random())).AsSingle();

			if(IPA.Loader.PluginManager.GetPluginFromId("ChatCore") == null)
				return;

			Container.BindInterfacesAndSelfTo<RequestManager>().AsSingle().NonLazy();
		}
	}
}
