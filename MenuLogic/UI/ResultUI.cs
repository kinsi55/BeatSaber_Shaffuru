using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using Shaffuru.AppLogic;
using Shaffuru.GameLogic;
using TMPro;
using UnityEngine;

namespace Shaffuru.MenuLogic.UI {
	[HotReload(RelativePathToLayout = @"Views/result.bsml")]
	[ViewDefinition("Shaffuru.MenuLogic.UI.Views.result.bsml")]
	public class ResultUI : BSMLAutomaticViewController {
		public event Action closed;
		public event Action<IPreviewBeatmapLevel> playSong;

		[UIAction("closexd")] void closexd() => closed.Invoke();


		[UIComponent("sessionResult")] readonly TextMeshProUGUI sessionResult = null;
		[UIComponent("sessionLength")] readonly TextMeshProUGUI sessionLength = null;
		[UIComponent("songCount")] readonly TextMeshProUGUI songCount = null;
		[UIComponent("sessionAcc")] readonly TextMeshProUGUI sessionAcc = null;

		[UIComponent("songList")] public CustomCellListTableData songList = null;

		SongListSong lastSelectedSong;
		void SongSelected(TableView _, SongListSong row) {
			lastSelectedSong = row;
		}

		void PlaySelected() {
			playSong.Invoke(lastSelectedSong.preview);
		}

		class SongListSong {
			[UIComponent("cover")] readonly ImageView cover = null;

			readonly ShaffuruSong song;
			internal IPreviewBeatmapLevel preview { get; private set; }

			readonly string songName = "";
			readonly string playTime = "";
			readonly string diffAndSource = "";


			public SongListSong(ShaffuruSong song) {
				this.song = song;

				preview = BeatmapLoader.GetPreviewBeatmapFromLevelId(song.levelId);

				songName = preview?.songName ?? "Unknown song";
				playTime = string.Format(@" {0:mm\:ss} - {1:mm\:ss}", TimeSpan.FromSeconds(song.startTime), TimeSpan.FromSeconds(song.startTime + song.length));

				if(song.source == null) {
					diffAndSource = $"{(BeatmapDifficulty)song.diffIndex} (Randomly picked)";
				} else {
					diffAndSource = $"{(BeatmapDifficulty)song.diffIndex} (Added by {song.source})";
				}
			}

			[UIComponent("bgContainer")] readonly ImageView bg = null;

			[UIAction("#post-parse")]
			void Parsed() {
				bg.color = new Color(0, 0, 0, 0.55f);

				cover.sprite = SongCore.Loader.defaultCoverImage;
				preview?.GetCoverImageAsync(CancellationToken.None).ContinueWith(x => {
					cover.sprite = x.Result;
				}, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
			}

			[UIAction("refresh-visuals")]
			void RefreshBgState(bool selected, bool highlighted) {
				bg.color = new Color(0, 0, 0, selected ? 0.9f : highlighted ? 0.8f : 0.55f);
			}
		}

		public static LevelCompletionResults levelCompletionResult;
		PlayedSongList playedSongList;

		[UIAction("#post-parse")]
		void Parsed() {
			if(songList == null)
				return;

			var passed = levelCompletionResult.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared &&
				levelCompletionResult.energy > 0;

			sessionResult.color = passed ? Color.green : Color.red;
			sessionResult.text = passed ? "Passed PogU" : "Failed FeelsBadMan";

			var ts = TimeSpan.FromSeconds(levelCompletionResult.endSongTime);

			sessionLength.text = $"Session Length: {ts.Minutes + (ts.Hours * 60)}:{ts.Seconds}";

			var totalnotes = levelCompletionResult.goodCutsCount + levelCompletionResult.badCutsCount + levelCompletionResult.missedCount;
			songCount.text = $"Songs Played: {playedSongList.list.Count} ({totalnotes} Notes)";

			var score = levelCompletionResult.totalCutScore == 0 ? 0f : levelCompletionResult.multipliedScore / levelCompletionResult.maxCutScore;

			sessionAcc.text = string.Format("Score: {0:n0}", levelCompletionResult.multipliedScore);

			songList.data.Clear();
			songList.tableView.ReloadData();


			void SpawnTable() {
				songList.data = playedSongList.list.Select(x => new SongListSong(x)).ToList<object>();
				songList.tableView.ReloadData();

				songList.tableView.SelectCellWithIdx(0, true);
			}

			void SpawnTableWhenSongsLoaded(SongCore.Loader _, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> _2) {
				SongCore.Loader.SongsLoadedEvent -= SpawnTableWhenSongsLoaded;
				SpawnTable();
			}

			if(!SongCore.Loader.AreSongsLoading) {
				SpawnTable();
				return;
			}

			SongCore.Loader.SongsLoadedEvent += SpawnTableWhenSongsLoaded;
		}


		internal void Setup(LevelCompletionResults levelCompletionResults, PlayedSongList playedSongList) {
			ResultUI.levelCompletionResult = levelCompletionResults;
			this.playedSongList = playedSongList;
			Parsed();
		}
	}
}
