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

		static Action show;

		public void Initialize() {
			if(show == null)
				MenuButtons.instance.RegisterButton(new MenuButton("Shaffuru", " iufrkjedsfios", () => show(), true));
			show = ShowFlow;
		}
	}
}
