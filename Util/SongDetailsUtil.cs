using System.Threading.Tasks;
using SongDetailsCache;

namespace Shaffuru.Util {
	public static class SongDetailsUtil {
		public static SongDetails instance = null;

		public static async Task<SongDetails> Init() => instance ??= await SongDetails.Init();
	}
}
