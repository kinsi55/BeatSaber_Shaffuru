using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace Shaffuru.AppLogic {
	[ProtoContract(SkipConstructor = true)]
	class ShaffuruSong {
		[ProtoMember(1)]
		public string levelId { get; protected set; }
		[ProtoMember(2)]
		public int diffIndex { get; protected set; } = -1;
		[ProtoMember(3)]
		public float startTime { get; protected set; }
		[ProtoMember(4)]
		public float length { get; protected set; }
		[ProtoMember(5)]
		public string source { get; protected set; }

		public ShaffuruSong(string levelId, int diffIndex, float startTime = -1, float length = -1, string source = null) {
			this.levelId = levelId;
			this.diffIndex = diffIndex;
			this.startTime = startTime;
			this.length = length;
			this.source = source;
		}

		public ShaffuruSong(string levelId, BeatmapDifficulty diff, float startTime = -1, float length = -1, string source = null) :
			this(levelId, (int)diff, startTime, length, source) { }
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
