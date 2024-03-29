﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using Shaffuru.AppLogic;
using TMPro;
using UnityEngine;
using Zenject;

namespace Shaffuru.MenuLogic.UI {
	[HotReload(RelativePathToLayout = @"Views/setup.bsml")]
	[ViewDefinition("Shaffuru.MenuLogic.UI.Views.setup.bsml")]
	class SetupUI : BSMLAutomaticViewController {
		static Config config => Config.Instance;
		static SongFilteringConfig songFilterConfig => Config.Instance.songFilteringConfig;

		[Inject] readonly MapPool mapPool = null;
		[Inject] readonly SongQueue songQueue = null;
		[InjectOptional] readonly RequestManager requestManager = null;
		[Inject] readonly Anlasser anlasser = null;
		[Inject] readonly PlayedSongList playedSongList = null;

		[UIParams] readonly BSMLParserParams parserParams = null;

		string filter_playlist { get => Config.Instance.filter_playlist; set => Config.Instance.filter_playlist = value; }
		[UIValue("playlists")] List<object> playlists = null;
		[UIComponent("dropdown_playlist")] readonly DropDownListSetting playlistDropdown = null;


		[UIValue("hideOlderThanOptions")] static List<DateTime> hideOlderThanOptions => Config.hideOlderThanOptions;
		static int hideOlderThanOptionsCount => Config.hideOlderThanOptions.Count - 1;

		[UIAction("DateTimeToStr")] static string DateTimeToStr(int d) => hideOlderThanOptions[d].ToString("MMM yyyy", new CultureInfo("en-US"));
		internal int _hideOlderThan {
			get => Mathf.Clamp(config.songFilteringConfig.advanced_uploadDate_min, 0, hideOlderThanOptions.Count - 1);
			set => config.songFilteringConfig.advanced_uploadDate_min = Mathf.Clamp(value, 0, hideOlderThanOptions.Count - 1);
		}

		[UIAction("#post-parse")] void Parsed() => PreparePlaylistStuff(false);

		public void OnEnable() => PreparePlaylistStuff();

		void PreparePlaylistStuff(bool rebuild = true) {
			var hasLib = IPA.Loader.PluginManager.GetPluginFromId("BeatSaberPlaylistsLib") != null;
			if(playlistDropdown != null)
				playlistDropdown.interactable = hasLib;

			if(!rebuild)
				return;

			playlists = new List<object>() { Config.filter_playlist_default };

			if(hasLib)
				Task.Run(LoadPlaylistsList);
		}

		void LoadPlaylistsList() {
			playlists.AddRange(BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists(true).Select(x => x.packName).Distinct());

			if(playlistDropdown != null) {
				playlistDropdown.UpdateChoices();
				playlistDropdown.ReceiveValue();
			}
		}

		[UIComponent("button_advancedFiltersConfig")] readonly NoTransitionsButton advancedFiltersConfigButton = null;

		bool filter_enableAdvancedFilters {
			get => config.songFilteringConfig.enableAdvancedFilters;
			set {
				if(advancedFiltersConfigButton != null)
					advancedFiltersConfigButton.interactable = value;
				config.songFilteringConfig.enableAdvancedFilters = value;
			}
		}

		[UIAction("ClearQueue")]
		void ClearQueue() {
			songQueue.Clear();
		}

		[UIComponent("label_songCount")] readonly TextMeshProUGUI filteredSongsLabel = null;
		[UIComponent("button_startLevel")] readonly NoTransitionsButton startLevelButton = null;


		Action playButtonHandler;
		internal void SetPlayHandler(Action playButtonHandler) => this.playButtonHandler = playButtonHandler;

		[UIAction("PlayClicked")]
		async void PlayClicked() {
			if(playButtonHandler != null) {
				playButtonHandler();
				return;
			}

			startLevelButton.interactable = false;
			filteredSongsLabel.text = $"Filtering levels...";
			parserParams.EmitEvent("OpenStartModal");
			var playable = 0;
			try {
				mapPool.SetFilterConfig(Config.Instance.songFilteringConfig);
				await Task.Run(() => mapPool.ProcessBeatmapPool());
				playable = (mapPool?.filteredLevels?.Count ?? 0);
				filteredSongsLabel.text = $"{playable} Playable Levels ({(mapPool?.requestableLevels?.Count ?? 0)} on BeatSaver / requestable)";
			} catch(Exception ex) {
				filteredSongsLabel.text = $"Failed to build map pool";
				Plugin.Log.Error("Failed to build map pool:");
				Plugin.Log.Error(ex);
				return;
			}

			requestManager?.SetDefaultHandler();
			startLevelButton.interactable = playable > 0;

			if(playable > 0) {
				// We cannot require 2 songs to be played if there is only one..
				songQueue.SetRequeueBlockListSize(Math.Min(playable - 1, Config.Instance.queue_requeueLimit));
			}
		}

		[UIValue("playDuration")] int playDuration = 3;
		string PlayTimeFormatter(int mins) => mins == 69 ? "69 Nice!" : mins == 96 ? "96.41 的笑容都没你的" : $"{mins} Minutes";

		[UIAction("StartGame")]
		void StartGame() {
			playedSongList.Clear();
			anlasser.Start(60 * playDuration);
		}


		readonly string version = $"Version {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)} by Kinsi55";

		[UIComponent("sponsorsText")] readonly CurvedTextMeshPro sponsorsText = null;
		void OpenSponsorsLink() => Process.Start("https://github.com/sponsors/kinsi55");
		void OpenSponsorsModal() {
			sponsorsText.text = "Loading...";
			Task.Run(() => {
				var desc = "Failed to load";
				try {
					desc = (new WebClient()).DownloadString("http://kinsi.me/sponsors/bsout.php");
				} catch { }

				_ = IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() => {
					sponsorsText.text = desc;
					// There is almost certainly a better way to update / correctly set the scrollbar size...
					sponsorsText.gameObject.SetActive(false);
					sponsorsText.gameObject.SetActive(true);
				});
			}).ConfigureAwait(false);
		}
	}
}
