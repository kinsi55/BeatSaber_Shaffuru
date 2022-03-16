using Shaffuru.MenuLogic;
using SiraUtil.Zenject;
using SongDetailsCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Shaffuru.AppLogic {
	public class MapPool : IDisposable {
		readonly BeatmapLevelsModel beatmapLevelsModel;
		static System.Random rngSource;

		public static SongDetails songDetails;

		public MapPool(BeatmapLevelsModel beatmapLevelsModel, UBinder<Plugin, System.Random> rng) {
			this.beatmapLevelsModel = beatmapLevelsModel;
			rngSource = rng.Value;
		}

		public struct ValidSong {
			public IPreviewBeatmapLevel level;
			// I really didnt want a ref type per song just to store the valid diffs, so I store all valid diffs as a bitsum
			public int validDiffs;

			public BeatmapDifficulty GetRandomValidDiff() {
				var start =
					Config.Instance.random_prefer_top_diff ? 0 : rngSource.Next((int)BeatmapDifficulty.ExpertPlus);

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

		public IPreviewBeatmapLevel[] allLevels { get; private set; }
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
			allLevels = null;
		}

		public void Dispose() => Clear();

		// Wrapping this to prevent missing symbol stuff if no bsplaylistlib
		static class TheJ {
			public static ConditionalWeakTable<IPreviewBeatmapLevel, BeatmapDifficulty[]> GetAllSongsInSelectedPlaylist() {
				// This implementation kinda pains me from an overhead standpoint but its the simplest I could come up with
				var x = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager
					.GetAllPlaylists(true)
					.FirstOrDefault(x => x.packName == Config.Instance.filter_playlist);

				IEnumerable<IGrouping<IPreviewBeatmapLevel, BeatSaberPlaylistsLib.Types.PlaylistSong>> theThing = null;

				if(x is BeatSaberPlaylistsLib.Legacy.LegacyPlaylist l) {
					theThing = l.BeatmapLevels.Cast<BeatSaberPlaylistsLib.Types.PlaylistSong>().GroupBy(x => x.PreviewBeatmapLevel);
				} else if(x is BeatSaberPlaylistsLib.Blist.BlistPlaylist bl) {
					theThing = bl.BeatmapLevels.Cast<BeatSaberPlaylistsLib.Blist.BlistPlaylistSong>().GroupBy(x => x.PreviewBeatmapLevel);
				} else {
					return null;
				}

				var playlistSongs = new ConditionalWeakTable<IPreviewBeatmapLevel, BeatmapDifficulty[]>();

				foreach(var xy in theThing) {
					if(!Config.Instance.filter_playlist_onlyHighlighted) {
						playlistSongs.Add(xy.First().PreviewBeatmapLevel, null);
						continue;
					}

					var highlightedDiffs = xy.Where(x => x.Difficulties != null)
						.SelectMany(x => x.Difficulties)
						.Select(x => x.BeatmapDifficulty)
						.Distinct().ToArray();

					playlistSongs.Add(
						xy.First().PreviewBeatmapLevel,

						highlightedDiffs.Length == 0 ? null : highlightedDiffs
					);
				}

				return playlistSongs;
			}
		}

		public async Task ProcessBeatmapPool() {
			var minLength = Config.Instance.jumpcut_enabled ? Math.Max(Config.Instance.filter_minSeconds, Config.Instance.jumpcut_minSeconds) : Config.Instance.filter_minSeconds;

			var maps = beatmapLevelsModel
				.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks
				.Where(x => !(x is PreviewBeatmapLevelPackSO))
				.SelectMany(x => x.beatmapLevelCollection.beatmapLevels);

			allLevels = maps.ToArray();

			maps = maps.Where(x => x.songDuration - x.songTimeOffset >= minLength);

			ConditionalWeakTable<IPreviewBeatmapLevel, BeatmapDifficulty[]> playlistSongs = null;

			if(IPA.Loader.PluginManager.GetPluginFromId("BeatSaberPlaylistsLib") != null)
				playlistSongs = TheJ.GetAllSongsInSelectedPlaylist();

			var allowME = Config.Instance.filter_AllowME && IPA.Loader.PluginManager.GetPluginFromId("MappingExtensions") != null;

			var newFilteredLevels = new List<ValidSong>();

			foreach(var map in maps) {
				BeatmapDifficulty[] playlistDiffs = null;

				if(playlistSongs?.TryGetValue(map, out playlistDiffs) == false)
					continue;

				var songHash = GetHashOfPreview(map);
				Dictionary<string, SongCore.Data.ExtraSongData.DifficultyData> mappedExtraData = null;

#if PRE1_19
				if(songHash != null && map is CustomPreviewBeatmapLevel customMap) {
					var extraData = SongCore.Collections.RetrieveExtraSongData(songHash, customMap.customLevelPath);
#else
				if(songHash != null) {
					var extraData = SongCore.Collections.RetrieveExtraSongData(songHash);
#endif

					if(extraData?._difficulties == null)
						continue;

					mappedExtraData = new Dictionary<string, SongCore.Data.ExtraSongData.DifficultyData>();

					foreach(var x in extraData._difficulties) {
						var k = $"{x._beatmapCharacteristicName}_{x._difficulty}";

						if(!mappedExtraData.ContainsKey(k))
							mappedExtraData[k] = x;
					}
				}

				songDetails ??= await SongDetails.Init();

				foreach(var beatmapSet in map.previewDifficultyBeatmapSets) {
					// For now we limit to just Standard characteristic. This might not be necessary
					if(beatmapSet.beatmapCharacteristic != Anlasser.standardCharacteristic)
						continue;

					var songDetailsSong = SongDetailsCache.Structs.Song.none;

					// If advanced filters are on the song needs to exist in SongDetails.. because we need that info to filter with
					if(Config.Instance.filter_enableAdvancedFilters) {
						if(songHash == null || !songDetails.songs.FindByHash(songHash, out songDetailsSong))
							continue;

						if(songDetailsSong.bpm < Config.Instance.filter_advanced_bpm_min)
							continue;

						if(Config.Instance.filter_advanced_uploadDate_min > 0 &&
							songDetailsSong.uploadTime < Config.hideOlderThanOptions[Config.Instance.filter_advanced_uploadDate_min])
							continue;
					}

					var validSonge = new ValidSong() {
						level = map
					};

					foreach(var beatmapDiff in beatmapSet.beatmapDifficulties) {
						// playlistDiffs is only created if the playlist entry actually has any highlit diffs (And the option is enabled)
						if(playlistDiffs?.Contains(beatmapDiff) == false)
							continue;

						// mappedExtraData will be null for OST
						if(mappedExtraData != null) {
							if(!mappedExtraData.TryGetValue($"{beatmapSet.beatmapCharacteristic.serializedName}_{beatmapDiff}", out var extradata))
								continue;

							// I have a feeling any requirements in the map would be BAAAD
							if(extradata.additionalDifficultyData._requirements.Any(x => !allowME || x != "Mapping Extensions"))
								continue;
						}

						if(Config.Instance.filter_enableAdvancedFilters) {
							var diffIsValid = false;
							for(var i = (int)songDetailsSong.diffOffset + songDetailsSong.diffCount; --i >= songDetailsSong.diffOffset;) {
								var diff = songDetails.difficulties[i];

								if((int)diff.difficulty != (int)beatmapDiff)
									continue;

								if(diff.njs < Config.Instance.filter_advanced_njs_min || diff.njs > Config.Instance.filter_advanced_njs_max)
									break;

								var nps = (float)diff.notes / songDetailsSong.songDurationSeconds;
								if(nps < Config.Instance.filter_advanced_nps_min || nps > Config.Instance.filter_advanced_nps_max)
									break;

								if(Config.Instance.filter_advanced_only_ranked && !diff.ranked)
									break;

								diffIsValid = true;
							}

							if(!diffIsValid)
								continue;
						}

						validSonge.SetDiffValid(beatmapDiff);
					}

					if(validSonge.validDiffs != 0)
						newFilteredLevels.Add(validSonge);
				}
			}

			this.filteredLevels = newFilteredLevels.ToArray();

			var requestableLevels = new Dictionary<string, int>();

			for(var i = 0; i < filteredLevels.Length; i++) {
				var mapHash = GetHashOfPreview(filteredLevels[i].level);

				if(mapHash == null || !songDetails.songs.FindByHash(mapHash, out var song))
					continue;

				requestableLevels[mapHash] = i;
			}

			this.requestableLevels = requestableLevels;
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

		// Removes WIP etc from the end of the levelID that SongCore happens to add for duplicate songs with the same hash
		public static string GetLevelIdWithoutUniquenessAddition(string levelid) {
			if(levelid.Length <= 53)
				return levelid;

			return levelid.Substring(0, 53);
		}
	}
}
