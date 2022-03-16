using SiraUtil.Zenject;
using System;
using System.Text;
using UnityEngine;

namespace Shaffuru.MenuLogic {
	class Anlasser {
		public const string LevelId = "___Shaffuru";

		readonly PlayerDataModel playerDataModel;
		readonly MenuTransitionsHelper menuTransitionsHelper;

		static BeatmapLevelSO beatmapLevel;
		static BeatmapDataSO beatmapLevelData;
		static BeatmapLevelSO.DifficultyBeatmap difficultyBeatmap;
		static UBinder<Plugin, System.Random> rngSource;


		static EnvironmentInfoSO defaultEnvironment;
		public static BeatmapCharacteristicSO standardCharacteristic { get; private set; }

		public Anlasser(
			PlayerDataModel playerDataModel,
			CustomLevelLoader customLevelLoader,
			MenuTransitionsHelper menuTransitionsHelper,
			BeatmapCharacteristicCollectionSO beatmapCharacteristicCollectionSO,
			UBinder<Plugin, System.Random> rng
		) {
			this.playerDataModel = playerDataModel;
			this.menuTransitionsHelper = menuTransitionsHelper;

			beatmapLevel ??= ScriptableObject.CreateInstance<BeatmapLevelSO>();
			beatmapLevelData ??= ScriptableObject.CreateInstance<BeatmapDataSO>();
			difficultyBeatmap ??= new BeatmapLevelSO.DifficultyBeatmap(beatmapLevel, BeatmapDifficulty.ExpertPlus, 0, 10, 0, beatmapLevelData);

			rngSource = rng;

			defaultEnvironment ??= customLevelLoader.LoadEnvironmentInfo("", false);
			standardCharacteristic = beatmapCharacteristicCollectionSO.GetBeatmapCharacteristicBySerializedName("Standard");
		}

		public event Action<LevelCompletionResults> finishedOrFailedCallback;

		public void Start(int lengthSeconds, int rngSeed = 0) {
			beatmapLevel.beatmapLevelData.audioClip?.UnloadAudioData();

			var audioClip = AudioClip.Create("testSound", 1000 * lengthSeconds, 1, 1000, false);
			// I SetData() an empty float array of matching size before but apparently thats not necessary

			if(rngSeed != 0)
				rngSource.Value = new System.Random(rngSeed);

			var bpm = 13.37f;
			var comedy = (DateTime.Now.Day == 1 && DateTime.Now.Month == 4) || rngSource.Value.NextDouble() >= 0.98;
			var a = (bpm * ((lengthSeconds - 1) / 60)).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

			if(comedy) {
				beatmapLevelData.SetJsonData(@"{""version"":""3.0.0"",""basicBeatmapEvents"":[{""i"":3,""f"":3}],""colorNotes"":[{""b"":0.33,""x"":1,""y"":1,""d"":8},{""b"":" + a + @",""x"":1,""y"":1,""d"":8}],""bombNotes"":[],""obstacles"":[{""b"":0.33,""y"":2,""d"":0.02,""w"":1,""h"":-3},{""b"":0.33,""x"":2,""y"":2,""d"":0.02,""w"":1,""h"":-3},{""b"":0.33,""y"":2,""d"":0.02,""w"":3,""h"":1},{""b"":0.33,""x"":1,""y"":0,""d"":0.02,""w"":1,""h"":1},{""b"":0.33,""x"":3,""d"":0.02,""w"":1,""h"":2},{""b"":" + a + @",""x"":0,""d"":0.03,""w"":4,""h"":5}],""bpmEvents"":[],""rotationEvents"":[],""sliders"":[],""burstSliders"":[],""waypoints"":[],""colorBoostBeatmapEvents"":[],""lightColorEventBoxGroups"":[],""lightRotationEventBoxGroups"":[],""basicEventTypesWithKeywords"":[],""useNormalEventsAsCompatibleEvents"":true}");
			} else {
				beatmapLevelData.SetJsonData(@"{""_version"":""2.2.0"",""_events"":[{""_value"":3}],""_notes"":[{""_time"":0.5,""_lineIndex"":1,""_cutDirection"":1},{""_time"":0.5,""_lineIndex"":1,""_lineLayer"":1,""_cutDirection"":1},{""_time"":0.5,""_lineIndex"":1,""_lineLayer"":2,""_cutDirection"":1},{""_time"":0.5,""_lineIndex"":2,""_type"":1,""_cutDirection"":8},{""_time"":" + a + @"}],""_obstacles"":[{""_time"":0.4,""_lineIndex"":3,""_duration"":3,""_width"":1},{""_time"":0.4,""_duration"":3,""_width"":1},{""_time"":" + a + @",""_duration"":0.1,""_width"":4}],""_waypoints"":[]}");
			}

			beatmapLevel.InitFull(
				LevelId,
				comedy ? "Susfuru" : "Shaffuru",
				"",
				"Kinsi55",
				"Kinsi55",
				audioClip,
				bpm,
				0,
				0,
				0,
				0,
				0,
				SongCore.Loader.defaultCoverImage,
				defaultEnvironment,
				defaultEnvironment,
				new[] { new BeatmapLevelSO.DifficultyBeatmapSet(standardCharacteristic, new[] { difficultyBeatmap }) }
			);

			//var x = new GameplayModifiers(false, false, GameplayModifiers.EnergyType.Bar, true, false, false, GameplayModifiers.EnabledObstacleType.All,
			//	false, false, false, false, GameplayModifiers.SongSpeed.Normal, false, false, false, false, false);

			//var l =

			menuTransitionsHelper.StartStandardLevel(
				"Shaffuru",
				difficultyBeatmap,
				beatmapLevel,
				playerDataModel.playerData.overrideEnvironmentSettings,
				playerDataModel.playerData.colorSchemesSettings.GetOverrideColorScheme(),
				playerDataModel.playerData.gameplayModifiers,
				playerDataModel.playerData.playerSpecificSettings, // gameplaySetupViewController.playerSettings is only initialized after entering solo once
				null,
				"Exit",
				false,
				false,
				null,
				(a, b) => {
					// TODO: Handle other cases in some way maybe? Some end stats screen?
					if(b.levelEndAction == LevelCompletionResults.LevelEndAction.Restart) {
						Start(lengthSeconds);
					} else if(b.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared || 
						b.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed ||
						// If user is dum dum and plays with nofail and then backs out to menu we show this too because we are nice :)
						b.energy == 0f
					) {
						finishedOrFailedCallback?.Invoke(b);
					}
				}
			);
		}
	}
}
