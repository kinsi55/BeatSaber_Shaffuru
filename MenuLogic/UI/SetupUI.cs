﻿using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using Shaffuru.AppLogic;
using Shaffuru.MenuLogic;
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

namespace Shaffuru.UI {
	[HotReload(RelativePathToLayout = @"setup.bsml")]
	[ViewDefinition("Shaffuru.UI.setup.bsml")]
	class SetupUI : BSMLAutomaticViewController {
		Config config;
		[Inject] readonly MapPool mapPool = null;
		[Inject] readonly SongQueueManager songQueueManager = null;
		[Inject] readonly Anlasser anlasser = null;

		[UIParams] readonly BSMLParserParams parserParams = null;

		string filter_playlist { get => config.filter_playlist; set => config.filter_playlist = value; }
		[UIValue("playlists")] List<object> playlists = null;
		[UIComponent("dropdown_playlist")] DropDownListSetting playlistDropdown = null;

		public void Awake() {
			config = Config.Instance;
		}

		public void OnEnable() {
			if(IPA.Loader.PluginManager.GetPluginFromId("BeatSaberPlaylistsLib") != null)
				LoadPlaylistsList();
		}

		void LoadPlaylistsList() {
			playlists = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists().Select(x => x.packName).Prepend("None (All Songs)").ToList<object>();
			if(playlistDropdown != null) {
				playlistDropdown.values = playlists;
				playlistDropdown.UpdateChoices();
			}
		}

		[UIAction("#post-parse")]
		public void Parsed() {
			//filteredSongsLabel.text = $"Playable Levels: {(mapPool?.filteredLevels?.Length ?? 0)} ({(mapPool?.requestableLevels?.Count ?? 0)} requestable)";
		}

		void ClearQueue() {
			songQueueManager.Clear();
		}

		[UIComponent("filteredSongsLabel")] TextMeshProUGUI filteredSongsLabel;
		[UIComponent("startLevelButton")] NoTransitionsButton startLevelButton;

		async void OpenStartModal() {
			playlists.Add("HIO");
			startLevelButton.interactable = false;
			filteredSongsLabel.text = $"Filtering levels...";
			parserParams.EmitEvent("OpenStartModal");
			var playable = 0;
			try {
				await mapPool.ProcessBeatmapPool();
				playable = (mapPool?.filteredLevels?.Length ?? 0);
				filteredSongsLabel.text = $"{playable} Playable Levels ({(mapPool?.requestableLevels?.Count ?? 0)} requestable)";
			} catch {
				filteredSongsLabel.text = $"Failed to build map pool";
				return;
			}

			startLevelButton.interactable = playable > 0;
		}

		int playDuration = 3;
		string PlayTimeFormatter(int mins) => mins == 69 ? "69 Nice!" : mins == 96 ? "96.41 的笑容都没你的" : $"{mins} Minutes";

		void StartGame() {
			anlasser.Start(60 * playDuration);
		}


		private readonly string version = $"Version {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)} by Kinsi55";

		[UIComponent("sponsorsText")] CurvedTextMeshPro sponsorsText = null;
		void OpenSponsorsLink() => Process.Start("https://github.com/sponsors/kinsi55");
		void OpenSponsorsModal() {
			sponsorsText.text = "Loading...";
			Task.Run(() => {
				string desc = "Failed to load";
				try {
					desc = (new WebClient()).DownloadString("http://kinsi.me/sponsors/bsout.php");
				} catch { }

				_ = IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() => {
					sponsorsText.text = desc;
				});
			}).ConfigureAwait(false);
		}
	}
}
