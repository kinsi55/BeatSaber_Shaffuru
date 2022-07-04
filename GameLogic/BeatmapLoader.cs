using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static BeatmapLevelsModel;

namespace Shaffuru.GameLogic {
	class BeatmapLoader {
		readonly GameplayCoreSceneSetupData _sceneSetupData;
		static BeatmapLevelsModel beatmapLevelsModel;

		static readonly IPA.Utilities.FieldAccessor<BeatmapLevelsModel, Dictionary<string, IPreviewBeatmapLevel>>.Accessor BeatmapLevelsModel_loadedPreviewBeatmapLevels =
			IPA.Utilities.FieldAccessor<BeatmapLevelsModel, Dictionary<string, IPreviewBeatmapLevel>>.GetAccessor("_loadedPreviewBeatmapLevels");

		public BeatmapLoader(
			GameplayCoreSceneSetupData _sceneSetupData,
			BeatmapLevelsModel beatmapLevelsModel
		) {
			this._sceneSetupData = _sceneSetupData;
			BeatmapLoader.beatmapLevelsModel = beatmapLevelsModel;
		}

		public static void AddBeatmapToLoadedPreviewBeatmaps(string levelId, IPreviewBeatmapLevel level) {
			if(beatmapLevelsModel == null)
				return;

			BeatmapLevelsModel_loadedPreviewBeatmapLevels(ref beatmapLevelsModel)[levelId] = level;
		}

		public static IPreviewBeatmapLevel GetPreviewBeatmapFromLevelId(string levelId) {
			if(beatmapLevelsModel == null)
				return null;

			if(BeatmapLevelsModel_loadedPreviewBeatmapLevels(ref beatmapLevelsModel).TryGetValue(levelId, out var map))
				return map;

			return null;
		}


		public Task<GetBeatmapLevelResult> LoadBeatmap(string levelId) {
			return beatmapLevelsModel.GetBeatmapLevelAsync(levelId, CancellationToken.None);
		}

		public async Task<IReadonlyBeatmapData> TransformDifficulty(IDifficultyBeatmap difficulty) {
			var playerSpecificSettings = _sceneSetupData.playerSpecificSettings;
			var gameplayModifiers = _sceneSetupData.gameplayModifiers;

			// Process the new beatmap as tho we'd play it so LeftHanded etc is accounted for
			var environmentEffectsFilterPreset = (difficulty.difficulty == BeatmapDifficulty.ExpertPlus) ? playerSpecificSettings.environmentEffectsFilterExpertPlusPreset : playerSpecificSettings.environmentEffectsFilterDefaultPreset;
			return BeatmapDataTransformHelper.CreateTransformedBeatmapData(
				await difficulty.GetBeatmapDataAsync(_sceneSetupData.environmentInfo, playerSpecificSettings),
				difficulty.level,
				gameplayModifiers,
				playerSpecificSettings.leftHanded,
				environmentEffectsFilterPreset,
				_sceneSetupData.environmentInfo.environmentIntensityReductionOptions,
				// This is (currently) only used to decide if to merge walls or not...
				_sceneSetupData.mainSettingsModel
			);
		}
	}
}
