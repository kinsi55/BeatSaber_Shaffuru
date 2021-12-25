using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaffuru.AppLogic {
	public class ShaffuruSong {
		public string levelId { get; protected set; }
		public int diffIndex { get; protected set; } = -1;
		public float startTime { get; protected set; }
		public float length { get; protected set; }
		public string source { get; protected set; }

		public IPreviewBeatmapLevel level { get; protected set; }

		public ShaffuruSong(string levelId, int diffIndex, float startTime = -1, float length = -1, string source = null) {
			this.levelId = levelId;
			this.diffIndex = diffIndex;
			this.startTime = startTime;
			this.length = length;
			this.source = source;
		}

		public ShaffuruSong(string levelId, BeatmapDifficulty diff, float startTime = -1, float length = -1, string source = null) {
			this.levelId = levelId;
			this.diffIndex = (int)diff;
			this.startTime = startTime;
			this.length = length;
			this.source = source;
		}
	}

	class ShaffuruSongList {
		protected readonly List<ShaffuruSong> _list;

		public IReadOnlyList<ShaffuruSong> list => _list;

		public ShaffuruSongList(ShaffuruSong[] songs = null) {
			_list = songs?.ToList() ?? new List<ShaffuruSong>(69);
		}
	}

	class PlayedSongList : ShaffuruSongList {
		public void Clear() => _list.Clear();

		public void Add(ShaffuruSong song) => _list.Add(song);
	}
}
