﻿using System;
using Shaffuru.AppLogic;
using Shaffuru.Util;
using SiraUtil.Zenject;
using Zenject;

namespace Shaffuru.Installers {
	class AppInstaller : MonoInstaller {
		public override void InstallBindings() {
			Container.BindInstance(new UBinder<Plugin, Random>(new Random())).AsSingle();
			Container.Bind<Config>().FromInstance(Config.Instance ??= new Config()).AsSingle();

			Container.Bind<PlayedSongList>().FromInstance(new PlayedSongList()).AsSingle();

			Container.BindInterfacesAndSelfTo<MapPool>().AsSingle();
			Container.Bind<SongQueue>().AsSingle();

			if(IPA.Loader.PluginManager.GetPluginFromId("CatCore") != null) {
				Container.BindInterfacesAndSelfTo<CatCoreSource>().AsSingle();
			} else if(IPA.Loader.PluginManager.GetPluginFromId("BeatSaberPlusCORE")?.HVersion >= new Hive.Versioning.Version("3.4.1")) {
				Container.BindInterfacesAndSelfTo<BeatSaberPlusSource>().AsSingle();
			} else if(IPA.Loader.PluginManager.GetPluginFromId("ChatCore") != null) {
				Container.BindInterfacesAndSelfTo<ChatCoreSource>().AsSingle();
			}

			if(Container.HasBinding<IChatMessageSource>())
				Container.BindInterfacesAndSelfTo<RequestManager>().AsSingle().NonLazy();
		}
	}
}
