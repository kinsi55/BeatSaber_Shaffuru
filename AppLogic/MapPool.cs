using Shaffuru.MenuLogic;
using SongDetailsCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shaffuru.AppLogic {
	public class MapPool {
		readonly BeatmapLevelsModel beatmapLevelsModel;

		public static SongDetails songDetails;

		public MapPool(BeatmapLevelsModel beatmapLevelsModel) {
			this.beatmapLevelsModel = beatmapLevelsModel;
		}

		public struct ValidSong {
			public IPreviewBeatmapLevel level;
			// I really didnt want a ref type per song just to store the valid diffs, so I store all valid diffs as a bitsum
			public int validDiffs;

			public BeatmapDifficulty GetRandomValidDiff() {
				var start =
					Config.Instance.random_prefer_top_diff ? 0 :
					UnityEngine.Random.Range(0, (int)BeatmapDifficulty.ExpertPlus);

				var m = 1 + (int)BeatmapDifficulty.ExpertPlus;

				for(var i = m; i-- > 0;) {
					var x = (start + i) % m;

					if((validDiffs & (int)Math.Pow(2, x)) != 0)
						return (BeatmapDifficulty)x;
				}
				return BeatmapDifficulty.Easy;
			}

			public bool IsDiffValid(BeatmapDifficulty diff) => (validDiffs & (int)Math.Pow(2, (int)diff)) != 0;

			public void SetDiffValid(BeatmapDifficulty diff) {
				validDiffs |= (int)Math.Pow(2, (int)diff);
			}
		}

		public ValidSong[] filteredLevels { get; private set; }
		public IReadOnlyDictionary<string, int> requestableLevels { get; private set; }

		public bool HasLevelId(string levelId) => requestableLevels.ContainsKey(levelId);
		public string GetHashFromBeatsaverId(string mapKey) {
			if(songDetails != null && songDetails.songs.FindByMapId(mapKey, out var song) == true)
				return song.hash;
			return null;
		}

		public void Clear() {
			filteredLevels = null;
			requestableLevels = null;
		}

		IPreviewBeatmapLevel[] GetLevelsOfPlaylist() {
			var x = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists().FirstOrDefault(x => x.packName == Config.Instance.filter_playlist);

			return x?.beatmapLevelCollection?.beatmapLevels;
		}

		IEnumerable<IPreviewBeatmapLevel> GetLevels() {
			IEnumerable<IPreviewBeatmapLevel> ret = null;

			if(IPA.Loader.PluginManager.GetPluginFromId("BeatSaberPlaylistsLib") != null)
				ret = GetLevelsOfPlaylist();

			ret ??= beatmapLevelsModel.customLevelPackCollection.beatmapLevelPacks
				.SelectMany(x => x.beatmapLevelCollection.beatmapLevels);

			return ret;
		}

		public async Task ProcessBeatmapPool() {
			var minLength = Config.Instance.jumpcut_enabled ? Math.Max(Config.Instance.filter_minSeconds, Config.Instance.jumpcut_minSeconds) : Config.Instance.filter_minSeconds;

			//TODO: Option to Limit to playlist instead of customLevelPackCollection
			var maps = GetLevels().Where(x => x.songDuration - x.songTimeOffset > minLength);

			var newFilteredLevels = new List<ValidSong>();

			foreach(var map in maps) {
				var extraData = SongCore.Collections.RetrieveExtraSongData(GetHashOfPreview(map));

				if(extraData == null)
					continue;

				var mappedExtraData = extraData._difficulties.ToDictionary(x => $"{x._beatmapCharacteristicName}_{x._difficulty}");

				foreach(var beatmapSet in map.previewDifficultyBeatmapSets) {
					// For now we limit to just Standard characteristic. This might not be necessary
					if(beatmapSet.beatmapCharacteristic != Anlasser.standardCharacteristic)
						continue;

					var validSonge = new ValidSong() {
						level = map
					};

					foreach(var beatmapDiff in beatmapSet.beatmapDifficulties) {
						// Failsafe
						if(!mappedExtraData.TryGetValue($"{beatmapSet.beatmapCharacteristic.serializedName}_{beatmapDiff}", out var extradata))
							continue;

						// I have a feeling any requirements in the map would be BAAAD
						if(extradata.additionalDifficultyData._requirements.Length > 0)
							continue;

						//TODO: More filters? Here

						validSonge.SetDiffValid(beatmapDiff);
					}

					if(validSonge.validDiffs != 0)
						newFilteredLevels.Add(validSonge);
				}
			}

			this.filteredLevels = newFilteredLevels.ToArray();

			var requestableLevels = new Dictionary<string, int>();

			if(songDetails == null)
				songDetails = await SongDetails.Init();

			for(var i = 0; i < filteredLevels.Length; i++) {
				var mapHash = GetHashOfPreview(filteredLevels[i].level);

				if(mapHash == null || !songDetails.songs.FindByHash(mapHash, out var song))
					continue;

				requestableLevels[mapHash] = i;
			}

			this.requestableLevels = requestableLevels;

			//for(var mapIter = 0; mapIter < maps.Length; mapIter++) {
			//	var mapHash = GetHashOfPreview(maps[mapIter]);

			//	if(mapHash == null || !songDetails.songs.FindByHash(mapHash, out var song))
			//		continue;

			//	bool hadAny = false;

			//	var s = new ValidSong() {
			//		level = maps[i]
			//	};

			//	for(int i = (int)song.diffOffset; i < song.diffOffset + song.diffCount; i++) {
			//		var diff = songDetails.difficulties[i];

			//		// Checking this with SongDetails is doodoo - Should rather check on the map directly
			//		if(diff.notes == 0 || (diff.mods != SongDetailsCache.Structs.MapMods.Chroma && (int)diff.mods != 0))
			//			continue;

			//		var stars = diff.stars;

			//		if(stars < Config.Instance.filter_minStars || stars > Config.Instance.filter_maxStars)
			//			continue;

			//		hadAny = true;
			//		s.validDiffs |= (int)Math.Pow(2, (int)diff.difficulty);
			//	}

			//	if(hadAny)
			//		newFilteredLevels[mapHash] = s;
			//}
		}

		public static string GetHashOfPreview(IPreviewBeatmapLevel preview) {
			if(preview.levelID.Length < 53)
				return null;

			return GetHashOfLevelid(preview.levelID);
		}

		public static string GetHashOfLevelid(string levelid) {
			if(levelid[12] != '_') // custom_level_<hash, 40 chars>
				return null;

			return levelid.Substring(13, 40);
		}
	}
}
