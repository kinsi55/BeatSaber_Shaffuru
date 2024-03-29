﻿using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Polyglot;
using Shaffuru.GameLogic;
using SiraUtil.Zenject;
using UnityEngine;
using Zenject;

namespace Shaffuru.MenuLogic {
	class Anlasser {
		public const string LevelIdPrefix = "___Shaffuru_";

		readonly PlayerDataModel playerDataModel;
		readonly MenuTransitionsHelper menuTransitionsHelper;


		static int lastShaffuruMapLength = 0;
		public readonly static BeatmapLevelSO beatmapLevel = ScriptableObject.CreateInstance<BeatmapLevelSO>();
		readonly static BeatmapDataSO beatmapLevelData = ScriptableObject.CreateInstance<BeatmapDataSO>();
		public readonly static BeatmapLevelSO.DifficultyBeatmap difficultyBeatmap = new BeatmapLevelSO.DifficultyBeatmap(beatmapLevel, BeatmapDifficulty.ExpertPlus, 0, 10, 0, 0, beatmapLevelData);
		
		static UBinder<Plugin, System.Random> rngSource;


		static EnvironmentInfoSO defaultEnvironment;
		public static BeatmapCharacteristicSO standardCharacteristic { get; private set; }

		public Anlasser(
			PlayerDataModel playerDataModel,
			CustomLevelLoader customLevelLoader,
			[InjectOptional] MenuTransitionsHelper menuTransitionsHelper,
			BeatmapCharacteristicCollection beatmapCharacteristicCollection,
			UBinder<Plugin, System.Random> rng
		) {
			this.playerDataModel = playerDataModel;
			this.menuTransitionsHelper = menuTransitionsHelper;

			rngSource = rng;

			defaultEnvironment ??= customLevelLoader.LoadEnvironmentInfo("", false);
			standardCharacteristic = beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName("Standard");
		}

		public event Action<LevelCompletionResults> finishedOrFailedCallback;


		static readonly IPA.Utilities.FieldAccessor<BeatmapLevelData, AudioClip>.Accessor BeatmapLevelData_audioClip =
			IPA.Utilities.FieldAccessor<BeatmapLevelData, AudioClip>.GetAccessor("_audioClip");

		static readonly IPA.Utilities.FieldAccessor<BeatmapLevelSO, string>.Accessor BeatmapLevelSO_songName =
			IPA.Utilities.FieldAccessor<BeatmapLevelSO, string>.GetAccessor("_songName");

		static readonly IPA.Utilities.FieldAccessor<BeatmapLevelSO, string>.Accessor BeatmapLevelSO_levelID =
			IPA.Utilities.FieldAccessor<BeatmapLevelSO, string>.GetAccessor("_levelID");

		public void UpdateFakeBeatmap(int lengthSeconds) {
			if(lengthSeconds > 120 * 60)
				throw new ArgumentOutOfRangeException();

			// for ref access
			var beatmapLevel = Anlasser.beatmapLevel;

			var isFirst = beatmapLevel.songAudioClip == null;

			if(lastShaffuruMapLength != lengthSeconds) {
				if(beatmapLevel.songAudioClip != null)
					GameObject.Destroy(beatmapLevel.songAudioClip);

				var audioClip = AudioClip.Create("", lengthSeconds * 1000, 1, 1000, false);

				beatmapLevel._audioClip = audioClip;
				if(beatmapLevel.beatmapLevelData is BeatmapLevelData b)
					BeatmapLevelData_audioClip(ref b) = audioClip;
				lastShaffuruMapLength = lengthSeconds;
			}

			const float bpm = 13.37f;

			var comedy = (DateTime.Now.Day == 1 && DateTime.Now.Month == 4) || rngSource.Value.NextDouble() >= 0.98;
			var a = (bpm * ((lengthSeconds - 1) / 60f)).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

			if(comedy) {
				beatmapLevelData.SetJsonData(@"{""version"":""3.0.0"",""basicBeatmapEvents"":[{""i"":3,""f"":3}],""colorNotes"":[{""b"":0.33,""x"":1,""y"":1,""d"":8},{""b"":" + a + @",""x"":1,""y"":1,""d"":8}],""bombNotes"":[],""obstacles"":[{""b"":0.33,""y"":2,""d"":0.02,""w"":1,""h"":-3},{""b"":0.33,""x"":2,""y"":2,""d"":0.02,""w"":1,""h"":-3},{""b"":0.33,""y"":2,""d"":0.02,""w"":3,""h"":1},{""b"":0.33,""x"":1,""y"":0,""d"":0.02,""w"":1,""h"":1},{""b"":0.33,""x"":3,""d"":0.02,""w"":1,""h"":2},{""b"":" + a + @",""x"":0,""d"":0.03,""w"":4,""h"":5}],""bpmEvents"":[],""rotationEvents"":[],""sliders"":[],""burstSliders"":[],""waypoints"":[],""colorBoostBeatmapEvents"":[],""lightColorEventBoxGroups"":[],""lightRotationEventBoxGroups"":[],""basicEventTypesWithKeywords"":[],""useNormalEventsAsCompatibleEvents"":true}");
			} else {
				beatmapLevelData.SetJsonData(@"{""_version"":""2.2.0"",""_events"":[{""_value"":3}],""_notes"":[{""_time"":0.5,""_lineIndex"":1,""_cutDirection"":1},{""_time"":0.5,""_lineIndex"":1,""_lineLayer"":1,""_cutDirection"":1},{""_time"":0.5,""_lineIndex"":1,""_lineLayer"":2,""_cutDirection"":1},{""_time"":0.5,""_lineIndex"":2,""_type"":1,""_cutDirection"":8},{""_time"":" + a + @"}],""_obstacles"":[{""_time"":0.4,""_lineIndex"":3,""_duration"":3,""_width"":1},{""_time"":0.4,""_duration"":3,""_width"":1},{""_time"":" + a + @",""_duration"":0.1,""_width"":4}],""_waypoints"":[]}");
			}

			if(isFirst) {
				beatmapLevel.InitFull(
					LevelIdPrefix,
					"Shaffuru",
					"",
					"Kinsi55",
					"Kinsi55",
					beatmapLevel.songAudioClip,
					bpm,
					0,
					0,
					0,
					0,
					0,
					SongCore.Loader.defaultCoverImage,
					defaultEnvironment,
					defaultEnvironment,
					new EnvironmentInfoSO[] { },
					new[] { new BeatmapLevelSO.DifficultyBeatmapSet(standardCharacteristic, new[] { difficultyBeatmap }) }
				);
			}

			BeatmapLevelSO_songName(ref beatmapLevel) = $"Shaffuru ({lengthSeconds / 60} Minutes)";
			BeatmapLevelSO_levelID(ref beatmapLevel) = $"{LevelIdPrefix}{lengthSeconds}";
		}

		static MethodInfo StartStandardLevelMethod = typeof(MenuTransitionsHelper).GetMethods().FirstOrDefault(
			x => x.Name == nameof(MenuTransitionsHelper.StartStandardLevel) &&
			x.GetParameters().Any(x => x.Name == "afterSceneSwitchCallback")
		);

		public void Start(int lengthSeconds, int rngSeed = 0) {
			if(rngSeed != 0)
				rngSource.Value = new System.Random(rngSeed);

			UpdateFakeBeatmap(lengthSeconds);

			var endCallback = new Action<LevelScenesTransitionSetupDataSO, LevelCompletionResults>((a, b) => {
				if(b.levelEndAction == LevelCompletionResults.LevelEndAction.Restart) {
					Start(lengthSeconds);
					return;
				}

				BeatmapLoader.RefreshLevelPacksIfNecessary();

				if(b.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared ||
					b.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed ||
					// If user is dum dum and plays with nofail and then backs out to menu we show this too because we are nice :)
					b.energy == 0f
				) {
					finishedOrFailedCallback?.Invoke(b);
				}
			});

			menuTransitionsHelper.StartStandardLevel(
				"Shaffuru",
				difficultyBeatmap,
				beatmapLevel,
				playerDataModel.playerData.overrideEnvironmentSettings,
				playerDataModel.playerData.colorSchemesSettings.GetOverrideColorScheme(),
				playerDataModel.playerData.colorSchemesSettings.GetOverrideColorScheme(),
				playerDataModel.playerData.gameplayModifiers,
				playerDataModel.playerData.playerSpecificSettings,
				null,
				Localization.Get("BUTTON_MENU"),
				false,
				false,
				null,
				endCallback,
				null
			);
		}
	}
}
