using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using HMUI;
using Zenject;
using System;

namespace Shaffuru.UI {
	class SetupFlowCoordinator : FlowCoordinator, IInitializable {
		[Inject] SetupUI ui = null;
		[Inject] GameplaySetupViewController gameplaySetupViewController = null;

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) {
			Console.WriteLine("SetupUI {0} gameplaySetupViewController {1}", ui, gameplaySetupViewController);
			if(firstActivation) {
				SetTitle("Shaffuru Setup");
				showBackButton = true;

				gameplaySetupViewController.Setup(true, true, true, false, PlayerSettingsPanelController.PlayerSettingsPanelLayout.Singleplayer);
			}

			if(addedToHierarchy) {
				ProvideInitialViewControllers(ui, gameplaySetupViewController);
			}
		}

		protected override void BackButtonWasPressed(ViewController topViewController) {
			BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this, null, ViewController.AnimationDirection.Horizontal);
		}

		public void ShowFlow() {
			var _parentFlow = BeatSaberUI.MainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();

			BeatSaberUI.PresentFlowCoordinator(_parentFlow, this);
		}

		public void Initialize() {
			MenuButtons.instance.RegisterButton(new MenuButton("Shaffuru", " iufrkjedsfios", ShowFlow, true));
		}
	}
}
