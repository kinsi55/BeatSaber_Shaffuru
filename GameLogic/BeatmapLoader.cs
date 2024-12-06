using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static BeatmapLevelsModel;

namespace Shaffuru.GameLogic {
	class BeatmapLoader {
		readonly GameplayCoreSceneSetupData _sceneSetupData;
		static BeatmapLevelsModel beatmapLevelsModel;

		static BeatmapLevelLoader beatmapLevelLoader;
		static BeatmapDataLoader beatmapDataLoader;
		static BeatmapLevelsEntitlementModel beatmapLevelsEntitlementModel;
		static bool reloadLevelpacksOnExit = false;

		public BeatmapLoader(
			GameplayCoreSceneSetupData _sceneSetupData,
			BeatmapLevelsModel beatmapLevelsModel,

			BeatmapLevelLoader beatmapLevelLoader,
			BeatmapDataLoader beatmapDataLoader,
			BeatmapLevelsEntitlementModel beatmapLevelsEntitlementModel
		) {
			this._sceneSetupData = _sceneSetupData;
			BeatmapLoader.beatmapLevelsModel = beatmapLevelsModel;

			BeatmapLoader.beatmapLevelLoader = beatmapLevelLoader;
			BeatmapLoader.beatmapDataLoader = beatmapDataLoader;
			BeatmapLoader.beatmapLevelsEntitlementModel = beatmapLevelsEntitlementModel;
		}

		public static void AddBeatmapToLoadedPreviewBeatmaps(string levelId, BeatmapLevel level) {
			beatmapLevelsModel._allLoadedBeatmapLevelsRepository.AddBeatmapLevel(level, SongCore.Loader.CustomLevelsPack.packID);

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




		public async Task<IReadonlyBeatmapData> LoadBeatmap(string levelId, BeatmapCharacteristicSO characteristicSO, BeatmapDifficulty difficulty) {
			var key = new BeatmapKey(levelId, characteristicSO, difficulty);

			var beatmapLevelData = (await beatmapLevelLoader.LoadBeatmapLevelDataAsync(
				levelId, 
				BeatmapLevelDataVersion.Original, // ??????? Only god knows what this is for
				CancellationToken.None
			)).beatmapLevelData;

			//beatmapLevelData.

			return beatmapDataLoader.LoadBeatmapDataAsync(
				beatmapLevelData, 
				key,
				beatmapLevelData.beatmapLevelData
			);
		}

		public async Task<IReadonlyBeatmapData> TransformDifficulty(IDifficultyBeatmap difficulty) {
			var playerSpecificSettings = _sceneSetupData.playerSpecificSettings;
			var gameplayModifiers = _sceneSetupData.gameplayModifiers;

			// Process the new beatmap as tho we'd play it so LeftHanded etc is accounted for
			var environmentEffectsFilterPreset = (difficulty.difficulty == BeatmapDifficulty.ExpertPlus) ? playerSpecificSettings.environmentEffectsFilterExpertPlusPreset : playerSpecificSettings.environmentEffectsFilterDefaultPreset;
			return BeatmapDataTransformHelper.CreateTransformedBeatmapData(
				await difficulty.GetBeatmapDataAsync(_sceneSetupData.targetEnvironmentInfo, playerSpecificSettings),
				difficulty.level,
				gameplayModifiers,
				playerSpecificSettings.leftHanded,
				environmentEffectsFilterPreset,
				_sceneSetupData.targetEnvironmentInfo.environmentIntensityReductionOptions,
				// This is (currently) only used to decide if to merge walls or not...
				_sceneSetupData.playerSpecificSettings
			);
		}
	}
}
