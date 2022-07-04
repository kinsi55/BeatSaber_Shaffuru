using System.Reflection;
using BeatSaberMarkupLanguage;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using Shaffuru.AppLogic;
using SiraUtil.Zenject;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace Shaffuru {
	[Plugin(RuntimeOptions.SingleStartInit)]
	public class Plugin {
		internal static Plugin Instance { get; private set; }
		internal static IPALogger Log { get; private set; }
		internal static Harmony harmony { get; private set; }

		internal static bool isShaffuruActive = false;

		[Init]
		public Plugin(IPALogger logger, IPA.Config.Config conf, Zenjector zenjector) {
			Instance = this;
			Log = logger;

			Config.Instance = conf.Generated<Config>();
			zenjector.Install<Installers.AppInstaller>(Location.App);
			zenjector.Install<Installers.MenuInstaller>(Location.Menu);
			zenjector.Install<Installers.GameInstaller>(Location.StandardPlayer);
		}

		[OnStart]
		public void OnApplicationStart() {
			harmony = new Harmony("Kinsi55.BeatSaber.Shaffuru");
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			var Tex2D = new Texture2D(2, 2);
			Tex2D.LoadImage(Utilities.GetResource(Assembly.GetExecutingAssembly(), "Shaffuru.Assets.SongCoreFolder.png"));

			var sp = Sprite.Create(Tex2D, new Rect(0, 0, Tex2D.width, Tex2D.height), Vector2.zero, 100);

			SongCore.Collections.AddSeperateSongFolder("Shaffuru Downloads", SongDownloaderJob.ShaffuruDownloadPath, SongCore.Data.FolderLevelPack.NewPack, sp);
		}

		[OnExit]
		public void OnApplicationQuit() {
			harmony.UnpatchSelf();
		}
	}
}
