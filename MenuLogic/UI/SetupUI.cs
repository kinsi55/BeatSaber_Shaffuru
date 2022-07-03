using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using Shaffuru.AppLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using TMPro;
using Zenject;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Components.Settings;
using UnityEngine;
using System.Globalization;

namespace Shaffuru.MenuLogic.UI {
	[HotReload(RelativePathToLayout = @"Views/setup.bsml")]
	[ViewDefinition("Shaffuru.MenuLogic.UI.Views.setup.bsml")]
	class SetupUI : BSMLAutomaticViewController {
		Config config;
		[Inject] readonly MapPool mapPool = null;
		[Inject] readonly SongQueueManager songQueueManager = null;
		[Inject] readonly Anlasser anlasser = null;
		[Inject] readonly PlayedSongList playedSongList = null;

		[UIParams] readonly BSMLParserParams parserParams = null;

		string filter_playlist { get => config.filter_playlist; set => config.filter_playlist = value; }
		[UIValue("playlists")] List<object> playlists = null;
		[UIComponent("dropdown_playlist")] readonly DropDownListSetting playlistDropdown = null;


		[UIValue("hideOlderThanOptions")] static List<DateTime> hideOlderThanOptions => Config.hideOlderThanOptions;
		static int hideOlderThanOptionsCount => Config.hideOlderThanOptions.Count - 1;

		[UIAction("DateTimeToStr")] static string DateTimeToStr(int d) => hideOlderThanOptions[d].ToString("MMM yyyy", new CultureInfo("en-US"));
		internal int _hideOlderThan {
			get => Mathf.Clamp(Config.Instance.filter_advanced_uploadDate_min, 0, hideOlderThanOptions.Count - 1);
			set => Config.Instance.filter_advanced_uploadDate_min = Mathf.Clamp(value, 0, hideOlderThanOptions.Count - 1);
		}

		public void Awake() {
			config = Config.Instance;
		}

		[UIAction("#post-parse")] void Parsed() => PreparePlaylistStuff(false);

		public void OnEnable() => PreparePlaylistStuff();

		void PreparePlaylistStuff(bool rebuild = true) {
			var hasLib = IPA.Loader.PluginManager.GetPluginFromId("BeatSaberPlaylistsLib") != null;
			if(playlistDropdown != null)
				playlistDropdown.interactable = hasLib;

			if(!rebuild)
				return;

			playlists = new List<object>() { "None (All Songs)" };

			if(hasLib)
				LoadPlaylistsList();
		}

		void LoadPlaylistsList() {
			playlists.AddRange(BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists(true).Select(x => x.packName).Distinct());

			if(playlistDropdown != null)
				playlistDropdown.UpdateChoices();
		}

		[UIComponent("button_advancedFiltersConfig")] readonly NoTransitionsButton advancedFiltersConfigButton = null;

		bool filter_enableAdvancedFilters {
			get => config.filter_enableAdvancedFilters;
			set {
				if(advancedFiltersConfigButton != null)
					advancedFiltersConfigButton.interactable = value;
				config.filter_enableAdvancedFilters = value;
			}
		}

		[UIAction("ClearQueue")]
		void ClearQueue() {
			songQueueManager.Clear();
		}

		[UIComponent("label_songCount")] readonly TextMeshProUGUI filteredSongsLabel = null;
		[UIComponent("button_startLevel")] readonly NoTransitionsButton startLevelButton = null;

		async void OpenStartModal() {
			startLevelButton.interactable = false;
			filteredSongsLabel.text = $"Filtering levels...";
			parserParams.EmitEvent("OpenStartModal");
			var playable = 0;
			try {
				await Task.Run(mapPool.ProcessBeatmapPool);
				playable = (mapPool?.filteredLevels?.Count ?? 0);
				filteredSongsLabel.text = $"{playable} Playable Levels ({(mapPool?.requestableLevels?.Count ?? 0)} on BeatSaver / requestable)";
			} catch(Exception ex) {
				filteredSongsLabel.text = $"Failed to build map pool";
				Plugin.Log.Error("Failed to build map pool:");
				Plugin.Log.Error(ex);
				return;
			}

			startLevelButton.interactable = playable > 0;

			if(playable > 0) {
				// We cannot require 2 songs to be played if the is only one..
				SongQueueManager.requeueBlockList.SetSize(Math.Min(playable - 1, Config.Instance.queue_requeueLimit));
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
