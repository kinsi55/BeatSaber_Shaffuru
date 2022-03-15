using HarmonyLib;
using Shaffuru.MenuLogic;

namespace Shaffuru.HarmonyPatches {
	[HarmonyPatch(typeof(SongCore.Collections), nameof(SongCore.Collections.RetrieveDifficultyData))]
	static class SongCoreRequirementData {
		static readonly SongCore.Data.ExtraSongData.DifficultyData difficultyData = new SongCore.Data.ExtraSongData.DifficultyData {
			additionalDifficultyData = new SongCore.Data.ExtraSongData.RequirementData {
				_requirements = new[] { "Mapping Extensions" }
			}
		};

		static void Postfix(IDifficultyBeatmap beatmap, ref SongCore.Data.ExtraSongData.DifficultyData __result) {
			if(beatmap.level.levelID != Anlasser.LevelId)
				return;

			__result = difficultyData;
		}
	}
}
