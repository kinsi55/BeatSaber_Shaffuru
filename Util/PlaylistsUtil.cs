using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Shaffuru.Util {
	static class PlaylistsUtil {
		public static ConditionalWeakTable<IPreviewBeatmapLevel, BeatmapDifficulty[]> GetAllSongsInPlaylist(string playlistName) {
			// This implementation kinda pains me from an overhead standpoint but its the simplest I could come up with
			var x = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager
				.GetAllPlaylists(true)
				.FirstOrDefault(x => x.packName == playlistName);

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
					.SelectMany(x => x.Difficulties);

				playlistSongs.Add(
					xy.First().PreviewBeatmapLevel,

					!highlightedDiffs.Any() ? null : highlightedDiffs.Select(x => x.BeatmapDifficulty).Distinct().ToArray()
				);
			}

			return playlistSongs;
		}
	}
}
