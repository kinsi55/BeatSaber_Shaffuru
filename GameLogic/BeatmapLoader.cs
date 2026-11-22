using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shaffuru.MenuLogic;
using UnityEngine;

namespace Shaffuru.GameLogic {
	// Replacement for the removed IDifficultyBeatmap, exposing what we used of it
	class LoadedDifficultyBeatmap {
		public BeatmapLevel level;
		public BeatmapKey beatmapKey;
		public BeatmapBasicData beatmapBasicData;
		public IBeatmapLevelData beatmapLevelData;
		public AudioClip audioClip;

		public BeatmapDifficulty difficulty => beatmapKey.difficulty;
		public float noteJumpMovementSpeed => beatmapBasicData.noteJumpMovementSpeed;
		public float noteJumpStartBeatOffset => beatmapBasicData.noteJumpStartBeatOffset;
	}

	class BeatmapLoader {
		readonly GameplayCoreSceneSetupData _sceneSetupData;
		static BeatmapLevelsModel beatmapLevelsModel;
		static bool reloadLevelpacksOnExit = false;

		public BeatmapLoader(
			GameplayCoreSceneSetupData _sceneSetupData,
			BeatmapLevelsModel beatmapLevelsModel
		) {
			this._sceneSetupData = _sceneSetupData;
			BeatmapLoader.beatmapLevelsModel = beatmapLevelsModel;
		}

		public static void AddBeatmapToLoadedPreviewBeatmaps(string levelId, BeatmapLevel level) {
			beatmapLevelsModel._allLoadedBeatmapLevelsRepository._idToBeatmapLevel[levelId] = level;

			reloadLevelpacksOnExit = true;
		}

		public static BeatmapLevel GetPreviewBeatmapFromLevelId(string levelId) {
			if(beatmapLevelsModel._allLoadedBeatmapLevelsRepository.TryGetBeatmapLevelById(levelId, out var map))
				return map;

			return null;
		}

		public static bool RefreshLevelPacksIfNecessary() {
			if(!reloadLevelpacksOnExit)
				return false;

			reloadLevelpacksOnExit = false;

			SongCore.Loader.Instance.RefreshSongs(false);

			return true;
		}




		public Task<LoadBeatmapLevelDataResult> LoadBeatmap(string levelId) {
			return beatmapLevelsModel.LoadBeatmapLevelDataAsync(levelId, BeatmapLevelDataVersion.Original, CancellationToken.None);
		}

		public LoadedDifficultyBeatmap GetDifficulty(string levelId, BeatmapDifficulty difficulty, IBeatmapLevelData beatmapLevelData) {
			var level = GetPreviewBeatmapFromLevelId(levelId);
			var basicData = level?.GetDifficultyBeatmapData(Anlasser.standardCharacteristic, difficulty);

			if(basicData == null)
				return null;

			return new LoadedDifficultyBeatmap() {
				level = level,
				beatmapKey = new BeatmapKey(levelId, Anlasser.standardCharacteristic, difficulty),
				beatmapBasicData = basicData,
				beatmapLevelData = beatmapLevelData
			};
		}

		public async Task<IReadonlyBeatmapData> TransformDifficulty(LoadedDifficultyBeatmap difficulty) {
			var playerSpecificSettings = _sceneSetupData.playerSpecificSettings;
			var gameplayModifiers = _sceneSetupData.gameplayModifiers;

			difficulty.audioClip = await _sceneSetupData._audioClipAsyncLoader.LoadSong(difficulty.beatmapLevelData);

			// Process the new beatmap as tho we'd play it so LeftHanded etc is accounted for
			var environmentEffectsFilterPreset = (difficulty.difficulty == BeatmapDifficulty.ExpertPlus) ? playerSpecificSettings.environmentEffectsFilterExpertPlusPreset : playerSpecificSettings.environmentEffectsFilterDefaultPreset;
			return BeatmapDataTransformHelper.CreateTransformedBeatmapData(
				await _sceneSetupData._beatmapDataLoader.LoadBeatmapDataAsync(
					difficulty.beatmapLevelData,
					difficulty.beatmapKey,
					difficulty.level.beatsPerMinute,
					difficulty.beatmapBasicData.environmentName == _sceneSetupData.targetEnvironmentInfo.serializedName,
					_sceneSetupData.targetEnvironmentInfo,
					_sceneSetupData.originalEnvironmentInfo,
					BeatmapLevelDataVersion.Original,
					gameplayModifiers,
					playerSpecificSettings,
					false
				),
				difficulty.level,
				gameplayModifiers,
				playerSpecificSettings.leftHanded,
				environmentEffectsFilterPreset,
				_sceneSetupData.originalEnvironmentInfo.environmentIntensityReductionOptions,
				// This is (currently) only used to decide if to merge walls or not...
				_sceneSetupData._settingsManager.settings
			);
		}
	}
}
