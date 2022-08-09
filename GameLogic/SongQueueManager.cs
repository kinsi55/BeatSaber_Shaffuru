using System;
using System.Collections.Generic;
using System.Linq;
using Shaffuru.AppLogic;
using Shaffuru.Util;
using SiraUtil.Zenject;
using Unity.Collections;
using Zenject;

namespace Shaffuru.GameLogic {
	class SongQueueManager : ISongQueueManager {
		[Inject] protected readonly SongQueue songQueue;
		[Inject] protected readonly UBinder<Plugin, System.Random> rngSource;
		[Inject] readonly MapPool mapPool = null;

		public virtual bool EnqueueSong(ShaffuruSong queuedSong) {
			return songQueue.EnqueueSong(queuedSong);
		}

		public virtual ShaffuruSong PickRandomValidSong() {
			using(var _levels = new NativeArray<MapPool.ValidSong>(mapPool.filteredLevels.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory)) {
				// I have no idea why I need to do this but else Compiler angy
				var levels = _levels;

				var validLevels = 0;

				foreach(var fl in mapPool.filteredLevels) {
					if(songQueue.IsInHistory(fl.level.levelID))
						continue;

					levels[validLevels++] = fl;
				}

				// Shouldnt ever be the case, failsafe
				if(validLevels == 0)
					return null;

				var l = levels[rngSource.Value.Next(validLevels)];

				return new ShaffuruSong(l.level.levelID, l.GetRandomValidDiff());
			}
		}

		public virtual ShaffuruSong GetNextSong() {
			ShaffuruSong x;

			if(songQueue.IsEmpty()) {
				// If the requeueBlockList contains as many levels as we have filtered ones, clear it, because every playable level has already been played
				if(mapPool.filteredLevels.Count == songQueue.requeueBlockList.Count)
					songQueue.requeueBlockList.Clear();

				x = PickRandomValidSong();
			} else {
				x = songQueue.DequeueSong(false);
			}

			songQueue.AddToHistory(x.levelId);

			return x;
		}
	}

	internal interface ISongQueueManager {
		public bool EnqueueSong(ShaffuruSong queuedSong);
		public ShaffuruSong GetNextSong();
		public ShaffuruSong PickRandomValidSong();
	}
}
