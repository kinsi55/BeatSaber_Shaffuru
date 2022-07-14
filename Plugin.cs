using System.Reflection;
using System.Runtime.CompilerServices;
using BeatSaberMarkupLanguage;
using IPA;
using Shaffuru.AppLogic;
using SiraUtil.Zenject;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

[assembly: InternalsVisibleTo("Shaffuru.Multiplayer", AllInternalsVisible = true)]
namespace Shaffuru {
	[Plugin(RuntimeOptions.SingleStartInit)]
	public class Plugin {
		internal static Plugin Instance { get; private set; }
		internal static IPALogger Log { get; private set; }

		[Init]
		public Plugin(IPALogger logger, Zenjector zenjector) {
			Instance = this;
			Log = logger;

			zenjector.Install<Installers.AppInstaller>(Location.App);
			zenjector.Install<Installers.MenuInstaller>(Location.Menu);
			zenjector.Install<Installers.GameInstaller>(Location.StandardPlayer);
		}

		[OnStart]
		public void OnApplicationStart() {
			var Tex2D = new Texture2D(2, 2);
			Tex2D.LoadImage(Utilities.GetResource(Assembly.GetExecutingAssembly(), "Shaffuru.Assets.SongCoreFolder.png"));

			var sp = Sprite.Create(Tex2D, new Rect(0, 0, Tex2D.width, Tex2D.height), Vector2.zero, 100);

			SongCore.Collections.AddSeperateSongFolder("Shaffuru Downloads", SongDownloaderJob.ShaffuruDownloadPath, SongCore.Data.FolderLevelPack.NewPack, sp);
		}

		[OnExit]
		public void OnExit() {
			Config.Instance?.Save();
		}
	}
}
