using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using Shaffuru.AppLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Zenject;

namespace Shaffuru.MenuLogic.UI {
	[HotReload(RelativePathToLayout = @"Views/result.bsml")]
	[ViewDefinition("Shaffuru.MenuLogic.UI.Views.result.bsml")]
	public class ResultUI : BSMLAutomaticViewController {
		public event Action closed;
		public event Action<IPreviewBeatmapLevel> playSong;

		[UIAction("closexd")] void closexd() => closed.Invoke();


		[UIComponent("sessionResult")] TextMeshProUGUI sessionResult = null;
		[UIComponent("sessionLength")] TextMeshProUGUI sessionLength = null;
		[UIComponent("songCount")] TextMeshProUGUI songCount = null;
		[UIComponent("sessionAcc")] TextMeshProUGUI sessionAcc = null;

		[UIComponent("songList")] public CustomCellListTableData songList = null;

		SongListSong lastSelectedSong;
		void SongSelected(TableView tableView, SongListSong row) {
			lastSelectedSong = row;
		}

		void PlaySelected() {
			playSong.Invoke(lastSelectedSong.preview);
		}

		class SongListSong {
			[UIComponent("cover")] ImageView cover = null;

			ShaffuruSong song = null;
			internal IPreviewBeatmapLevel preview { get; private set; }

			string songName = "";
			string playTime = "";
			string diffAndSource = "";


			public SongListSong(ShaffuruSong song) {
				this.song = song;

				preview = SongCore.Loader.BeatmapLevelsModelSO.GetLevelPreviewForLevelId(song.levelId);

				songName = preview?.songName ?? "Unknown song";
				playTime = string.Format(@" {0:mm\:ss} - {1:mm\:ss}", TimeSpan.FromSeconds(song.startTime), TimeSpan.FromSeconds(song.startTime + song.length));

				if(song.source == null) {
					diffAndSource = $"{(BeatmapDifficulty)song.diffIndex} (Randomly picked)";
				} else {
					diffAndSource = $"{(BeatmapDifficulty)song.diffIndex} (Added by {song.source})";
				}
			}

			[UIComponent("bgContainer")] ImageView bg = null;

			[UIAction("#post-parse")]
			void Parsed() {
				bg.color = new Color(0, 0, 0, 0.55f);

				cover.sprite = SongCore.Loader.defaultCoverImage;
				preview.GetCoverImageAsync(CancellationToken.None).ContinueWith(x => {
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

			sessionLength.text = $"Session Length: {TimeSpan.FromSeconds(levelCompletionResult.endSongTime).ToString(@"mm\:ss")}";

			var totalnotes = levelCompletionResult.goodCutsCount + levelCompletionResult.badCutsCount + levelCompletionResult.missedCount;
			songCount.text = $"Songs Played: {playedSongList.list.Count} ({totalnotes} Notes)";

			var maxScore = ScoreModel.MaxRawScoreForNumberOfNotes(totalnotes);
			var score = levelCompletionResult.rawScore == 0 ? 0f : levelCompletionResult.rawScore / maxScore;

			sessionAcc.text = string.Format("Overall Accuracy: {0:0.00%}", score);

			songList.data = playedSongList.list.Select(x => new SongListSong(x)).ToList<object>();
			songList.tableView.ReloadData();

			songList.tableView.SelectCellWithIdx(0, true);
		}



		internal void Setup(LevelCompletionResults levelCompletionResults, PlayedSongList playedSongList) {
			ResultUI.levelCompletionResult = levelCompletionResults;
			this.playedSongList = playedSongList;
			Parsed();
		}
	}
}
