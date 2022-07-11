using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaffuru.Util {
	static class MapUtil {
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
