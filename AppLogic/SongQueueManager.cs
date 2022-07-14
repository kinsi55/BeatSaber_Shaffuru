using System;
using System.Collections.Generic;
using System.Linq;
using Shaffuru.Util;
using SiraUtil.Zenject;
using Unity.Collections;
using Zenject;

namespace Shaffuru.AppLogic {
	class SongQueueManager : ISongQueueManager {
		protected static Queue<ShaffuruSong> queue;
		protected static RollingList<string> requeueBlockList;


		[Inject] protected readonly MapPool mapPool;
		[Inject] protected readonly UBinder<Plugin, System.Random> rngSource;

		public SongQueueManager() {
			queue ??= new Queue<ShaffuruSong>();

			if(requeueBlockList == null) {
				requeueBlockList = new RollingList<string>(Config.Instance.queue_requeueLimit);
			} else {
				SetRequeueBlockListSize(Config.Instance.queue_requeueLimit);
			}
		}

		public virtual void SetRequeueBlockListSize(int size) {
			requeueBlockList.SetSize(size);
		}

		public virtual bool EnqueueSong(ShaffuruSong queuedSong) {
			if(IsFull())
				return false;

			if(!mapPool.LevelHashRequestable(MapUtil.GetHashOfLevelid(queuedSong.levelId)))
				return false;

			lock(queue)
				queue.Enqueue(queuedSong);

			return true;
		}

		public virtual void Clear() => queue?.Clear();

		public virtual ShaffuruSong GetNextSong() {
			ShaffuruSong x;

			if(queue.Count == 0) {
				// If the requeueBlockList contains as many levels as we have filtered ones, clear it, because every playable level has already been played
				if(mapPool.filteredLevels.Count == requeueBlockList.Count)
					requeueBlockList.Clear();

				using(var _levels = new NativeArray<MapPool.ValidSong>(mapPool.filteredLevels.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory)) {
					// I have no idea why I need to do this but else Compiler angy
					var levels = _levels;

					var validLevels = 0;

					foreach(var fl in mapPool.filteredLevels) {
						if(requeueBlockList.Contains(fl.level.levelID))
							continue;

						levels[validLevels++] = fl;
					}

					// Shouldnt ever be the case, failsafe
					if(validLevels == 0)
						return null;

					var l = levels[rngSource.Value.Next(validLevels)];

					x = new ShaffuruSong(l.level.levelID, l.GetRandomValidDiff());
				}
			} else {
				lock(queue)
					x = queue.Dequeue();
			}

			requeueBlockList.Add(MapUtil.GetLevelIdWithoutUniquenessAddition(x.levelId));

			return x;
		}

		public virtual int Count(Func<ShaffuruSong, bool> action) => queue.Count(action);
		public virtual bool Contains(Func<ShaffuruSong, bool> action) => queue.Any(action);
		public virtual bool IsInHistory(string levelId) => requeueBlockList.Contains(MapUtil.GetLevelIdWithoutUniquenessAddition(levelId)) == true;
		public virtual bool IsEmpty() => queue.Count == 0;

		public virtual bool IsFull() => queue.Count >= Config.Instance.queue_sizeLimit;
	}

	internal interface ISongQueueManager {
		public void SetRequeueBlockListSize(int size);
		public bool EnqueueSong(ShaffuruSong queuedSong);
		public void Clear();
		public ShaffuruSong GetNextSong();

		public int Count(Func<ShaffuruSong, bool> action);
		public bool Contains(Func<ShaffuruSong, bool> action);
		public bool IsInHistory(string levelId);
		public bool IsEmpty();

		public bool IsFull();
	}
}
