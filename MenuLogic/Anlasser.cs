using System;
using System.Text;
using UnityEngine;

namespace Shaffuru.MenuLogic {
	class Anlasser {
		public const string LevelId = "___Shaffuru";

		PlayerDataModel playerDataModel;
		MenuTransitionsHelper menuTransitionsHelper;

		static BeatmapLevelSO beatmapLevel;
		static BeatmapDataSO beatmapLevelData;
		static BeatmapLevelSO.DifficultyBeatmap difficultyBeatmap;


		static EnvironmentInfoSO defaultEnvironment;
		public static BeatmapCharacteristicSO standardCharacteristic { get; private set; }

		public Anlasser(
			PlayerDataModel playerDataModel,
			CustomLevelLoader customLevelLoader,
			MenuTransitionsHelper menuTransitionsHelper,
			BeatmapCharacteristicCollectionSO beatmapCharacteristicCollectionSO
		) {
			this.playerDataModel = playerDataModel;
			this.menuTransitionsHelper = menuTransitionsHelper;

			beatmapLevel ??= ScriptableObject.CreateInstance<BeatmapLevelSO>();
			beatmapLevelData ??= ScriptableObject.CreateInstance<BeatmapDataSO>();
			difficultyBeatmap ??= new BeatmapLevelSO.DifficultyBeatmap(beatmapLevel, BeatmapDifficulty.ExpertPlus, 0, 10, 0, beatmapLevelData);

			defaultEnvironment ??= customLevelLoader.LoadEnvironmentInfo("", false);
			standardCharacteristic = beatmapCharacteristicCollectionSO.GetBeatmapCharacteristicBySerializedName("Standard");
		}

		public void Start(int lengthSeconds) {
			beatmapLevel.beatmapLevelData.audioClip?.UnloadAudioData();

			var audioClip = AudioClip.Create("testSound", 1000 * lengthSeconds, 1, 1000, false);
			// I SetData() an empty float array of matching size before but apparently thats not necessary

			var notes = new StringBuilder();

			var bpm = 13.37f;

			beatmapLevelData.SetJsonData(@"{""_version"":""2.2.0"",""_events"":[{""_type"":12}],""_notes"":[{""_time"":0.5,""_lineIndex"":1,""_cutDirection"":1},{""_time"":0.5,""_lineIndex"":1,""_lineLayer"":1,""_cutDirection"":1},{""_time"":0.5,""_lineIndex"":1,""_lineLayer"":2,""_cutDirection"":1},{""_time"":0.5,""_lineIndex"":2,""_type"":1,""_cutDirection"":8},{""_time"":801.75}],""_obstacles"":[{""_time"":0.4,""_lineIndex"":3,""_duration"":3,""_width"":1},{""_time"":0.4,""_duration"":3,""_width"":1},{""_time"":" + bpm * ((lengthSeconds - 1) / 60) + @",""_duration"":0.1,""_width"":4}],""_waypoints"":[]}");

			beatmapLevel.InitFull(
				LevelId,
				"Shaffuru",
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
				"Lmao",
				false,
				null,
				(a, b) => { Console.WriteLine("Finished"); }
			);
		}
	}
}
