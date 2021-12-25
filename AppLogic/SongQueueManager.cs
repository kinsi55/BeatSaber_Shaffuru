using Shaffuru.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shaffuru.AppLogic {
	class SongQueueManager {
		static Queue<ShaffuruSong> queue;
		public static RollingList<string> requeueBlockList { get; private set; }

		readonly MapPool mapPool;

		public SongQueueManager(MapPool mapPool) {
			this.mapPool = mapPool;

			queue ??= new Queue<ShaffuruSong>();

			if(requeueBlockList == null) {
				requeueBlockList = new RollingList<string>(Config.Instance.queue_requeueLimit);
			} else {
				requeueBlockList.SetSize(Config.Instance.queue_requeueLimit);
			}
		}

		public bool EnqueueSong(string levelId, BeatmapDifficulty difficulty, float startTime = -1, float length = -1, string source = null) {
			return EnqueueSong(new ShaffuruSong(levelId, difficulty, startTime, length, source));
		}

		public bool EnqueueSong(ShaffuruSong queuedSong) {
			if(IsFull())
				return false;

			if(mapPool.HasLevelId(queuedSong.levelId))
				return false;

			queue.Enqueue(queuedSong);
			return true;
		}

		public void Clear() => queue?.Clear();

		public ShaffuruSong GetNextSong() {
			ShaffuruSong x;

			if(queue.Count == 0) {
				var levels = mapPool.filteredLevels.Where(x => !SongQueueManager.requeueBlockList.Contains(x.level.levelID));

				// Shouldnt ever be the case, failsafe
				if(levels.Count() == 0)
					return null;

				// Basegame always Initializes the RNG with seed 0 on scene change.. That would be kinda not very RNG probably maybe. Cant hurt.
				UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

				var l = levels.ElementAt(UnityEngine.Random.Range(0, levels.Count()));

				x = new ShaffuruSong(l.level.levelID, l.GetRandomValidDiff(), -1, -1, null);
			} else {
				x = queue.Dequeue();
			}

			requeueBlockList.Add(MapPool.GetLevelIdWithoutUniquenessAddition(x.levelId));

			return x;
		}

		public int Count(Func<ShaffuruSong, bool> action) => queue.Count(action);
		public bool Contains(Func<ShaffuruSong, bool> action) => queue.Any(action);
		public bool IsInHistory(string levelId) => requeueBlockList.Contains(MapPool.GetLevelIdWithoutUniquenessAddition(levelId)) == true;
		public bool IsEmpty() => queue.Count == 0;

		public bool IsFull() => queue.Count >= Config.Instance.queue_sizeLimit;
	}
}
