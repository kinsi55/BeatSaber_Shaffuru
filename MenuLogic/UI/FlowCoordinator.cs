using System;
using System.Collections;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using HMUI;
using Shaffuru.AppLogic;
using UnityEngine;
using Zenject;
using static SelectLevelCategoryViewController;

namespace Shaffuru.MenuLogic.UI {
	class ShaffuruFlowCoordinator : FlowCoordinator, IInitializable {
		[Inject] readonly SetupUI ui = null;
		[Inject] readonly ResultUI resultui = null;
		[Inject] readonly GameplaySetupViewController gameplaySetupViewController = null;
		[Inject] readonly PlayedSongList playedSongList = null;
		[Inject] readonly Anlasser anlasser = null;

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) {
			if(firstActivation) {
				SetTitle("Shaffuru Setup");
				showBackButton = true;

				gameplaySetupViewController.Setup(true, true, true, false, PlayerSettingsPanelController.PlayerSettingsPanelLayout.Singleplayer);

				anlasser.finishedOrFailedCallback += ShowResultView;

				ProvideInitialViewControllers(ui, gameplaySetupViewController);

				resultui.closed += Resultui_closed;
				resultui.playSong += Resultui_playSong;
			}
		}

		private void Resultui_playSong(IPreviewBeatmapLevel obj) {
			BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this, immediately: true);
			SharedCoroutineStarter.instance.StartCoroutine(PlaySonge(obj));
		}

		[InjectOptional] readonly MainFlowCoordinator mainFlowCoordinator = null;
		[InjectOptional] readonly LevelSearchViewController levelSearchViewController = null;
		[Inject] readonly LevelFilteringNavigationController levelFilteringNavigationController = null;
		[InjectOptional] readonly LevelCollectionNavigationController levelCollectionNavigationController = null;

		IEnumerator PlaySonge(IPreviewBeatmapLevel songe) {
			mainFlowCoordinator.HandleMainMenuViewControllerDidFinish(null, MainMenuViewController.MenuButton.SoloFreePlay);
			yield return null;

			levelSearchViewController?.ResetCurrentFilterParams();
			levelFilteringNavigationController.UpdateCustomSongs();

			if(levelFilteringNavigationController.selectedLevelCategory.ToString() != nameof(LevelCategory.All))
				levelFilteringNavigationController.UpdateSecondChildControllerContent((LevelCategory)Enum.Parse(typeof(LevelCategory), nameof(LevelCategory.All)));

			yield return new WaitForEndOfFrame();
			// Reset again here. This is kind of a duct-tape fix for an edge-case of better song list
			levelSearchViewController?.ResetCurrentFilterParams();
			levelCollectionNavigationController?.SelectLevel(songe);
		}

		private void Resultui_closed() {
			SetTitle("Shaffuru Setup");
			ProvideInitialViewControllers(ui, gameplaySetupViewController);
			ReopenHack();
		}

		protected override void BackButtonWasPressed(ViewController topViewController) {
			BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
		}

		// HAHABALLS
		void ReopenHack() {
			BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this, immediately: true);
			ShowSetupView();
		}

		public void ShowResultView(LevelCompletionResults levelCompletionResults) {
			SetTitle("Shaffuru Result");
			resultui.Setup(levelCompletionResults, playedSongList);
			ProvideInitialViewControllers(resultui);
			ReopenHack();
		}

		static ShaffuruFlowCoordinator instance;

		public void Initialize() {
			if(instance == null)
				MenuButtons.instance.RegisterButton(new MenuButton("Shaffuru", " iufrkjedsfios", ShowSetupView, true));
			instance = this;
		}

		public void ShowSetupView() => ShowSetupView(null);
		public void ShowSetupView(Action playButtonHandler) {
			if(instance == null)
				return;

			ui.SetPlayHandler(playButtonHandler);

			var _parentFlow = BeatSaberUI.MainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();

			BeatSaberUI.PresentFlowCoordinator(_parentFlow, instance);
		}
	}
}
