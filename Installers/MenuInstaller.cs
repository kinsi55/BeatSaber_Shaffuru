using Shaffuru.MenuLogic;
using Shaffuru.UI;
using SiraUtil;
using Zenject;

namespace Shaffuru.Installers {
	class MenuInstaller : MonoInstaller {
		public override void InstallBindings() {
			Container.Bind<Anlasser>().AsSingle().NonLazy();
			Container.Bind<SetupUI>().FromNewComponentAsViewController().AsSingle();
			Container.Bind<IInitializable>().To<SetupFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
			//Container.Bind<FlowCoordinatorCoordinatorIHateBSUI>().AsSingle().NonLazy();
		}
	}
}
