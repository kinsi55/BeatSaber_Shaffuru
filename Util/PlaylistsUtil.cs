using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Shaffuru.Util {
	static class PlaylistsUtil {
		public static Dictionary<string, BeatmapDifficulty[]> GetAllSongsInPlaylist(string playlistName) {
			// This implementation kinda pains me from an overhead standpoint but its the simplest I could come up with
			var x = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager
				.GetAllPlaylists(true)
				.FirstOrDefault(x => x.Title == playlistName);

			IEnumerable<IGrouping<string, BeatSaberPlaylistsLib.Types.PlaylistSong>> theThing = null;

			if(x is BeatSaberPlaylistsLib.Legacy.LegacyPlaylist l) {
				theThing = l.Cast<BeatSaberPlaylistsLib.Types.PlaylistSong>().Where(x => x.BeatmapLevel != null).GroupBy(x => x.LevelId);
			} else if(x is BeatSaberPlaylistsLib.Blist.BlistPlaylist bl) {
				theThing = bl.Cast<BeatSaberPlaylistsLib.Blist.BlistPlaylistSong>().Where(x => x.BeatmapLevel != null).GroupBy(x => x.LevelId);
			} else {
				return null;
			}

			var playlistSongs = new Dictionary<string, BeatmapDifficulty[]>();

			foreach(var xy in theThing) {
				if(!Config.Instance.filter_playlist_onlyHighlighted) {
					playlistSongs.Add(xy.Key, null);
					continue;
				}

				var highlightedDiffs = xy.Where(x => x.Difficulties != null)
					.SelectMany(x => x.Difficulties);

				playlistSongs.Add(
					xy.Key,

					!highlightedDiffs.Any() ? null : highlightedDiffs.Select(x => x.BeatmapDifficulty).Distinct().ToArray()
				);
			}

			return playlistSongs;
		}
	}
}
