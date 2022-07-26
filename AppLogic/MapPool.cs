using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Shaffuru.MenuLogic;
using Shaffuru.Util;
using SiraUtil.Zenject;

namespace Shaffuru.AppLogic {
	class MapPool : IDisposable {
		readonly BeatmapLevelsModel beatmapLevelsModel;
		static System.Random rngSource;

		// This is for the SongDownloaderJob. Yes Unity says you should not access static variables. I have to
		public static MapPool instance;

		public MapPool(BeatmapLevelsModel beatmapLevelsModel, UBinder<Plugin, System.Random> rng) {
			this.beatmapLevelsModel = beatmapLevelsModel;
			rngSource = rng.Value;

			instance = this;
		}

		public struct ValidSong {
			public IPreviewBeatmapLevel level;
			// I really didnt want a ref type per song just to store the valid diffs, so I store all valid diffs as a bitsum
			public int validDiffs;

			public ValidSong(IPreviewBeatmapLevel level) {
				this.level = level;
				validDiffs = 0;
			}

			public BeatmapDifficulty GetRandomValidDiff() {
				var start =
					Config.Instance.random_prefer_top_diff ? 0 : rngSource.Next((int)BeatmapDifficulty.ExpertPlus);

				// I wish I commented how this works when I wrote it
				var m = 1 + (int)BeatmapDifficulty.ExpertPlus;

				for(var i = m; i-- > 0;) {
					var x = (start + i) % m;

					if((validDiffs & (1 << x)) != 0)
						return (BeatmapDifficulty)x;
				}
				return BeatmapDifficulty.Easy;
			}

			public bool IsDiffValid(BeatmapDifficulty diff) => (validDiffs & (1 << (int)diff)) != 0;
			public static bool IsDiffValid(int validDiffsMask, BeatmapDifficulty diff) => (validDiffsMask & (1 << (int)diff)) != 0;

			public void SetDiffValid(BeatmapDifficulty diff) {
				validDiffs |= (1 << (int)diff);
			}
		}

		public SongFilteringConfig currentFilterConfig;
		public List<ValidSong> filteredLevels { get; private set; }
		public IReadOnlyDictionary<string, int> requestableLevels { get; private set; }
		public bool isFilteredByPlaylist { get; private set; } = false;

		public bool LevelHashRequestable(string hash) => requestableLevels.ContainsKey(hash);
		public string GetHashFromBeatsaverId(string mapKey) {
			if(SongDetailsUtil.instance != null && SongDetailsUtil.instance.songs.FindByMapId(mapKey, out var song))
				return song.hash;
			return null;
		}

		public void Clear() {
			filteredLevels = null;
			requestableLevels = null;
		}

		public void Dispose() => Clear();


		int minSongLength = 0;
		public static bool supportsMappingExtensions => IPA.Loader.PluginManager.GetPluginFromId("MappingExtensions") != null;
		bool allowMappingExtensions = false;

		ValidSong LevelFilterCheck(IPreviewBeatmapLevel level, ConditionalWeakTable<IPreviewBeatmapLevel, BeatmapDifficulty[]> playlistSongs = null, bool forceNoFilters = false) {
			if(!forceNoFilters) {
				if(level.songDuration - level.songTimeOffset < minSongLength)
					return default;

				if(currentFilterConfig.enableAdvancedFilters && level.beatsPerMinute < currentFilterConfig.advanced_bpm_min)
					return default;
			}

			BeatmapDifficulty[] playlistDiffs = null;

			if(playlistSongs?.TryGetValue(level, out playlistDiffs) == false)
				return default;

			var songHash = MapUtil.GetHashOfPreview(level);
			Dictionary<BeatmapDifficulty, SongCore.Data.ExtraSongData.DifficultyData> mappedExtraData = null;

			if(songHash != null) {
				var extraData = SongCore.Collections.RetrieveExtraSongData(songHash);

				if(extraData?._difficulties == null)
					return default;

				foreach(var x in extraData._difficulties) {
					// We are only allowing Standard characteristic below - So we might as well only collect extradata for those
					if(x._beatmapCharacteristicName != Anlasser.standardCharacteristic.serializedName)
						continue;

					mappedExtraData ??= new Dictionary<BeatmapDifficulty, SongCore.Data.ExtraSongData.DifficultyData>();

					mappedExtraData[x._difficulty] = x;
				}
			}

			foreach(var beatmapSet in level.previewDifficultyBeatmapSets) {
				// For now we limit to just Standard characteristic. This might not be necessary
				if(beatmapSet.beatmapCharacteristic != Anlasser.standardCharacteristic)
					continue;

				var songDetailsSong = SongDetailsCache.Structs.Song.none;
				var validDiffsSongdetailsFilterCheck = int.MaxValue;

				// If advanced filters are on the song needs to exist in SongDetails.. because we need that info to filter with
				if(!forceNoFilters && currentFilterConfig.enableAdvancedFilters) {
					if(songHash == null || !SongDetailsUtil.instance.songs.FindByHash(songHash, out songDetailsSong))
						break;

					if(!SongdetailsFilterCheck(songDetailsSong, out validDiffsSongdetailsFilterCheck, false))
						break;
				}

				var validSonge = new ValidSong(level);

				foreach(var beatmapDiff in beatmapSet.beatmapDifficulties) {
					// playlistDiffs is only created if the playlist entry actually has any highlit diffs (And the option is enabled)
					if(playlistDiffs?.Contains(beatmapDiff) == false)
						continue;

					// mappedExtraData will be null for OST
					if(mappedExtraData != null) {
						if(!mappedExtraData.TryGetValue(beatmapDiff, out var extradata))
							continue;

						var r = extradata.additionalDifficultyData._requirements;

						// I have a feeling any requirements in the map would be BAAAD
						if(r.Length != 0 && (!allowMappingExtensions || Array.Exists(r, x => x != "Mapping Extensions")))
							continue;
					}

					validSonge.SetDiffValid(beatmapDiff);
				}

				validSonge.validDiffs &= validDiffsSongdetailsFilterCheck;

				return validSonge;
			}

			return default;
		}

		/// <param name="fullCheck">Can be used to disable some checks that would be redundant when called from LevelFilterCheck</param>
		public bool SongdetailsFilterCheck(in SongDetailsCache.Structs.Song song, out int validDiffs, bool fullCheck = true) {
			validDiffs = 0;

			if(fullCheck && song.songDurationSeconds < minSongLength)
				return false;

			if(currentFilterConfig.enableAdvancedFilters) {
				if(fullCheck && song.bpm < currentFilterConfig.advanced_bpm_min)
					return false;

				if(currentFilterConfig.advanced_uploadDate_min > 0 &&
					song.uploadTime < Config.hideOlderThanOptions[currentFilterConfig.advanced_uploadDate_min])
					return false;
			} else if(!fullCheck) {
				validDiffs = int.MaxValue;
				return true;
			}

			var __SongdetailsFilterCheckVS = new ValidSong();

			for(var i = (int)song.diffOffset + song.diffCount; --i >= song.diffOffset;) {
				ref var diff = ref SongDetailsUtil.instance.difficulties[i];

				if(diff.characteristic != SongDetailsCache.Structs.MapCharacteristic.Standard)
					continue;

				if(currentFilterConfig.enableAdvancedFilters) {
					if(diff.njs < currentFilterConfig.advanced_njs_min || diff.njs > currentFilterConfig.advanced_njs_max)
						continue;

					var nps = (float)diff.notes / song.songDurationSeconds;
					if(nps < currentFilterConfig.advanced_nps_min || nps > currentFilterConfig.advanced_nps_max)
						continue;

					if(currentFilterConfig.advanced_only_ranked && !diff.ranked)
						continue;
				}

				if(fullCheck) {
					if(!allowMappingExtensions && (diff.mods & SongDetailsCache.Structs.MapMods.MappingExtensions) != 0)
						continue;

					if((diff.mods & (SongDetailsCache.Structs.MapMods.NoodleExtensions | SongDetailsCache.Structs.MapMods.Chroma)) != 0)
						continue;
				}

				__SongdetailsFilterCheckVS.SetDiffValid((BeatmapDifficulty)diff.difficulty);
			}

			validDiffs = __SongdetailsFilterCheckVS.validDiffs;
			return validDiffs != 0;
		}


		public bool AddRequestableLevel(IPreviewBeatmapLevel level, bool forceNoFilters = false) {
			var hash = MapUtil.GetHashOfPreview(level);

			if(hash == null)
				return false;

			var levelCheck = LevelFilterCheck(level, forceNoFilters: forceNoFilters);

			if(levelCheck.validDiffs == 0)
				return false;

			lock(filteredLevels) {
				if(requestableLevels.ContainsKey(hash))
					return false;

				((Dictionary<string, int>)requestableLevels)[hash] = filteredLevels.Count;

				filteredLevels.Add(levelCheck);
			}

			return true;
		}

		public void SetFilterConfig(SongFilteringConfig config = null) {
			currentFilterConfig = config ?? Config.Instance.songFilteringConfig;
		}

		public async Task ProcessBeatmapPool(bool forceNoFilters = false) {
			minSongLength = Config.Instance.jumpcut_enabled ? Math.Max(currentFilterConfig.minSeconds, Config.Instance.jumpcut_minSeconds) : currentFilterConfig.minSeconds;

			var maps = beatmapLevelsModel
				.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks
				.Where(x => !(x is PreviewBeatmapLevelPackSO))
				.SelectMany(x => x.beatmapLevelCollection.beatmapLevels);

			if(!forceNoFilters)
				maps = maps.Where(x => x.songDuration - x.songTimeOffset >= minSongLength);


			ConditionalWeakTable<IPreviewBeatmapLevel, BeatmapDifficulty[]> playlistSongs = null;

			if(!forceNoFilters &&
				Config.Instance.filter_playlist != Config.filter_playlist_default && 
				IPA.Loader.PluginManager.GetPluginFromId("BeatSaberPlaylistsLib") != null
			)
				playlistSongs = PlaylistsUtil.GetAllSongsInPlaylist(Config.Instance.filter_playlist);

			isFilteredByPlaylist = playlistSongs != null;
			allowMappingExtensions = (forceNoFilters || currentFilterConfig.allowME) && supportsMappingExtensions;

			var newFilteredLevels = new List<ValidSong>();

			await SongDetailsUtil.Init();

			foreach(var map in maps) {
				var mapCheck = LevelFilterCheck(map, playlistSongs, forceNoFilters);

				if(mapCheck.validDiffs != 0)
					newFilteredLevels.Add(mapCheck);
			}

			this.filteredLevels = newFilteredLevels;

			var requestableLevels = new Dictionary<string, int>();

			for(var i = 0; i < filteredLevels.Count; i++) {
				var mapHash = MapUtil.GetHashOfPreview(filteredLevels[i].level);

				if(mapHash == null || !SongDetailsUtil.instance.songs.FindByHash(mapHash, out var song))
					continue;

				requestableLevels[mapHash] = i;
			}

			this.requestableLevels = requestableLevels;
		}
	}
}
