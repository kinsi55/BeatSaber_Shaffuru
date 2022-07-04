using Shaffuru.MenuLogic;
using Shaffuru.MenuLogic.UI;
using Zenject;

namespace Shaffuru.Installers {
	class MenuInstaller : MonoInstaller {
		public override void InstallBindings() {
			Container.Bind<Anlasser>().AsSingle().NonLazy();
			Container.Bind<SetupUI>().FromNewComponentAsViewController().AsSingle();
			Container.Bind<ResultUI>().FromNewComponentAsViewController().AsSingle();
			Container.Bind<IInitializable>().To<ShaffuruFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
			//Container.Bind<FlowCoordinatorCoordinatorIHateBSUI>().AsSingle().NonLazy();
		}
	}
}
