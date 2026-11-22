using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using BGLib.Polyglot;
using Shaffuru.GameLogic;
using SiraUtil.Zenject;
using UnityEngine;
using Zenject;

namespace Shaffuru.MenuLogic {
	static class BeatmapLevelDataSOExtensions {
		public static void SetJsonData(this BeatmapLevelDataSO beatmapLevelData, string json) {
			beatmapLevelData.SetDifficultyBeatmapSets(new[] {
				new BeatmapLevelDataSO.DifficultyBeatmapSet(Anlasser.standardCharacteristic.serializedName, new[] {
					new BeatmapLevelDataSO.DifficultyBeatmap(BeatmapDifficulty.ExpertPlus, new TextAsset(json))
				})
			});
		}
	}

	class Anlasser {
		public const string LevelIdPrefix = "___Shaffuru_";

		readonly PlayerDataModel playerDataModel;
		readonly MenuTransitionsHelper menuTransitionsHelper;
		readonly EnvironmentsListModel environmentsListModel;


		static int lastShaffuruMapLength = 0;
		public static BeatmapLevel beatmapLevel { get; private set; }
		readonly static BeatmapLevelDataSO beatmapLevelData = ScriptableObject.CreateInstance<BeatmapLevelDataSO>();
		
		static UBinder<Plugin, System.Random> rngSource;


		static string defaultEnvironment;
		public static BeatmapCharacteristicSO standardCharacteristic { get; private set; }

		public Anlasser(
			PlayerDataModel playerDataModel,
			CustomLevelLoader customLevelLoader,
			[InjectOptional] MenuTransitionsHelper menuTransitionsHelper,
			BeatmapCharacteristicCollection beatmapCharacteristicCollection,
			EnvironmentsListModel environmentsListModel,
			UBinder<Plugin, System.Random> rng
		) {
			this.playerDataModel = playerDataModel;
			this.menuTransitionsHelper = menuTransitionsHelper;
			this.environmentsListModel = environmentsListModel;

			rngSource = rng;

			customLevelLoader._defaultEnvironmentInfo.OperationHandle.WaitForCompletion();
			defaultEnvironment ??= customLevelLoader.CreateEnvironmentName("", customLevelLoader._defaultEnvironmentInfo);
			standardCharacteristic = beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName("Standard");
		}

		public event Action<LevelCompletionResults> finishedOrFailedCallback;

		public void UpdateFakeBeatmap(int lengthSeconds) {
			if(lengthSeconds > 120 * 60)
				throw new ArgumentOutOfRangeException();

			if(lastShaffuruMapLength != lengthSeconds) {
				if(beatmapLevelData.songAudioClip != null)
					GameObject.Destroy(beatmapLevelData.songAudioClip);

				var audioClip = AudioClip.Create("", lengthSeconds * 1000, 1, 1000, false);

				beatmapLevelData.SetAudioClip(audioClip);
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

			beatmapLevel = new BeatmapLevel(
				0,
				false,
				$"{LevelIdPrefix}{lengthSeconds}",
				$"Shaffuru ({lengthSeconds / 60} Minutes)",
				"",
				"Kinsi55",
				new[] { "Kinsi55" },
				new string[] { },
				bpm,
				-6f,
				0,
				0,
				0,
				lengthSeconds,
				PlayerSensitivityFlag.Safe,
				new StaticPreviewMediaData(SongCore.Loader.defaultCoverImage, beatmapLevelData.songAudioClip),
				new Dictionary<(BeatmapCharacteristicSO, BeatmapDifficulty), BeatmapBasicData>() {
					{ (standardCharacteristic, BeatmapDifficulty.ExpertPlus), new BeatmapBasicData(10, 0, defaultEnvironment, null, 0, 0, 0, 0, new[] { "Kinsi55" }, new string[] { }) }
				}
			);
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
				new BeatmapKey(beatmapLevel.levelID, standardCharacteristic, BeatmapDifficulty.ExpertPlus),
				beatmapLevel,
				beatmapLevelData,
				playerDataModel.playerData.overrideEnvironmentSettings,
				playerDataModel.playerData.colorSchemesSettings.GetOverrideColorScheme(),
				playerDataModel.playerData.colorSchemesSettings.ShouldOverrideLightshowColors(),
				playerDataModel.playerData.colorSchemesSettings.GetOverrideColorScheme(),
				playerDataModel.playerData.gameplayModifiers,
				playerDataModel.playerData.playerSpecificSettings,
				null,
				environmentsListModel,
				Localization.Get("BUTTON_MENU"),
				false,
				false,
				null,
				null,
				endCallback,
				null
			);
		}
	}
}
