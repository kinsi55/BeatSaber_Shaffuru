using System.Threading;
using System.Threading.Tasks;
using static BeatmapLevelsModel;

namespace Shaffuru.GameLogic {
	class BeatmapLoader {
		readonly GameplayCoreSceneSetupData _sceneSetupData;
		readonly BeatmapLevelsModel beatmapLevelsModel;
		public BeatmapLoader(
			GameplayCoreSceneSetupData _sceneSetupData,
			BeatmapLevelsModel beatmapLevelsModel
		) {
			this._sceneSetupData = _sceneSetupData;
			this.beatmapLevelsModel = beatmapLevelsModel;
		}

		public Task<GetBeatmapLevelResult> LoadBeatmap(string levelId) {
			return beatmapLevelsModel.GetBeatmapLevelAsync(levelId, CancellationToken.None);
		}

		public IReadonlyBeatmapData TransformDifficulty(IDifficultyBeatmap difficulty) {
			PlayerSpecificSettings playerSpecificSettings = _sceneSetupData.playerSpecificSettings;
			GameplayModifiers gameplayModifiers = _sceneSetupData.gameplayModifiers;

			// Process the new beatmap as tho we'd play it so LeftHanded etc is accounted for
			EnvironmentEffectsFilterPreset environmentEffectsFilterPreset = (difficulty.difficulty == BeatmapDifficulty.ExpertPlus) ? playerSpecificSettings.environmentEffectsFilterExpertPlusPreset : playerSpecificSettings.environmentEffectsFilterDefaultPreset;
			return BeatmapDataTransformHelper.CreateTransformedBeatmapData(
				difficulty.beatmapData,
				difficulty.level,
				gameplayModifiers,
				null,
				playerSpecificSettings.leftHanded,
				environmentEffectsFilterPreset,
				_sceneSetupData.environmentInfo.environmentIntensityReductionOptions,
				// This is (currently) only used to decide if to merge walls or not...
				false
			);
		}
	}
}
