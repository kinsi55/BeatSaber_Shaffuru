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

				anlasser.finishedOrFailedCallback += ShowResultView;

				ProvideInitialViewControllers(ui, gameplaySetupViewController);

				resultui.closed += Resultui_closed;
				resultui.playSong += Resultui_playSong;
			}
		}

		private void Resultui_playSong(IPreviewBeatmapLevel obj) {
			_parentFlow.DismissFlowCoordinator(this, immediately: true);
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

			if(levelFilteringNavigationController.selectedLevelCategory != LevelCategory.All)
				levelFilteringNavigationController.UpdateSecondChildControllerContent(LevelCategory.All);

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
			_parentFlow.DismissFlowCoordinator(this);
		}

		public static void Close(bool immediately, Action finishedCallback) {
			if(_parentFlow == null)
				return;

			_parentFlow.DismissFlowCoordinator(instance, finishedCallback, immediately: immediately);
			_parentFlow = null;
		}

		// HAHABALLS
		void ReopenHack() {
			_parentFlow.DismissFlowCoordinator(this, immediately: true);
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
				MenuButtons.instance.RegisterButton(new MenuButton("Shaffuru", " iufrkjedsfios", () => {
					gameplaySetupViewController.Setup(true, true, true, false, PlayerSettingsPanelController.PlayerSettingsPanelLayout.Singleplayer);
					ShowSetupView();
				}, true));
			instance = this;
		}

		static FlowCoordinator _parentFlow;
		public void ShowSetupView(Action playButtonHandler = null) {
			if(instance == null)
				return;

			ui.SetPlayHandler(playButtonHandler);

			_parentFlow = BeatSaberUI.MainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();

			BeatSaberUI.PresentFlowCoordinator(_parentFlow, instance);
		}
	}
}
