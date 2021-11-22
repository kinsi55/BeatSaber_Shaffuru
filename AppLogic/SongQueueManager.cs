using Shaffuru.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shaffuru.AppLogic {
	class SongQueueManager : IDisposable {
		public class QueuedSong {
			public string levelId { get; private set; }
			public int diffIndex { get; private set; } = -1;
			public float startTime { get; private set; }
			public float length { get; private set; }
			public string source { get; private set; }

			public QueuedSong(string levelId, int diffIndex, float startTime = -1, float length = -1, string source = null) {
				this.levelId = levelId;
				this.diffIndex = diffIndex;
				this.startTime = startTime;
				this.length = length;
				this.source = source;
			}

			public QueuedSong(string levelId, BeatmapDifficulty diff, float startTime = -1, float length = -1, string source = null) {
				this.levelId = levelId;
				this.diffIndex = (int)diff;
				this.startTime = startTime;
				this.length = length;
				this.source = source;
			}
		}

		Queue<QueuedSong> queue;
		public RollingList<string> history { get; private set; }

		readonly MapPool mapPool;

		public SongQueueManager(MapPool mapPool) {
			this.queue = new Queue<QueuedSong>();
			this.history = new RollingList<string>(Config.Instance.queue_requeueLimit);
			this.mapPool = mapPool;
		}

		public bool EnqueueSong(string levelId, BeatmapDifficulty difficulty, float startTime = -1, float length = -1, string source = null) {
			return EnqueueSong(new QueuedSong(levelId, difficulty, startTime, length, source));
		}
		public bool EnqueueSong(QueuedSong queuedSong) {
			if(IsFull())
				return false;

			if(mapPool.HasLevelId(queuedSong.levelId))
				return false;

			queue.Enqueue(queuedSong);
			return true;
		}

		public void Clear() => queue?.Clear();

		public QueuedSong DequeueSong() {
			if(queue.Count == 0)
				return null;

			var x = queue.Dequeue();

			history.Add(x.levelId.Length > 53 ? x.levelId.Substring(0, 53) : x.levelId);

			return x;
		}

		public int Count(Func<QueuedSong, bool> action) => queue.Count(action);
		public bool Contains(Func<QueuedSong, bool> action) => queue.Any(action);
		public bool IsInHistory(string levelId) => history?.Contains(levelId) == true;
		public bool IsEmpty() => queue.Count == 0;

		public bool IsFull() => queue.Count >= Config.Instance.queue_sizeLimit;


		public void Dispose() {
			queue = null;
		}
	}
}
