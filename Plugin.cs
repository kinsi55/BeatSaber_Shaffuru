using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using System.Reflection;
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
		}

		[OnExit]
		public void OnApplicationQuit() {
			harmony.UnpatchSelf();
		}
	}
}
