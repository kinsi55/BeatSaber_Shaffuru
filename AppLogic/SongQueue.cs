using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shaffuru.Util;
using Zenject;

namespace Shaffuru.AppLogic {
	class SongQueue {
		[Inject] readonly MapPool mapPool = null;

		static Queue<ShaffuruSong> _queue;
		static RollingList<string> _requeueBlockList;

		public Queue<ShaffuruSong> queue => _queue;
		public RollingList<string> requeueBlockList => _requeueBlockList;


		public SongQueue() {
			_queue ??= new Queue<ShaffuruSong>();

			if(_requeueBlockList == null) {
				_requeueBlockList = new RollingList<string>(Config.Instance.queue_requeueLimit);
			} else {
				SetRequeueBlockListSize(Config.Instance.queue_requeueLimit);
			}
		}

		public void SetRequeueBlockListSize(int size) {
			requeueBlockList.SetSize(size);
		}

		public bool EnqueueSong(ShaffuruSong queuedSong) {
			if(IsFull())
				return false;

			if(!mapPool.LevelHashRequestable(MapUtil.GetHashOfLevelid(queuedSong.levelId)))
				return false;

			lock(_queue)
				_queue.Enqueue(queuedSong);

			return true;
		}

		public ShaffuruSong DequeueSong(bool addToRequeueBlocklist = true) {
			ShaffuruSong s = null;
			lock(_queue)
				s = _queue.Dequeue();

			if(addToRequeueBlocklist && s != null)
				_requeueBlockList.Add(MapUtil.GetLevelIdWithoutUniquenessAddition(s.levelId));

			return s;
		}

		public void Clear() => _queue?.Clear();

		public int Count(Func<ShaffuruSong, bool> action) => _queue.Count(action);
		public bool Contains(Func<ShaffuruSong, bool> action) => _queue.Any(action);
		public bool IsInHistory(string levelId) => _requeueBlockList.Contains(MapUtil.GetLevelIdWithoutUniquenessAddition(levelId)) == true;
		public bool IsEmpty() => _queue.Count == 0;

		public bool IsFull() => _queue.Count >= Config.Instance.queue_sizeLimit;
	}
}
