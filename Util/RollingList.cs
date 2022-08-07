using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Shaffuru.Util {
	//https://stackoverflow.com/questions/14702654/rolling-list-in-net/18404024#18404024
	class RollingList<T> : IEnumerable<T> {
		private readonly ConcurrentStack<T> _list = new ConcurrentStack<T>();

		public RollingList(int maximumCount) {
			SetSize(maximumCount);
		}

		public void SetSize(int maximumCount) {
			this.maximumCount = maximumCount;

			while(_list.Count > this.maximumCount)
				_list.TryPop(out var _);
		}

		public int maximumCount { get; private set; }
		public int Count => _list.Count;

		public void Add(T value) {
			if(_list.Count >= maximumCount && _list.Count > 0)
				_list.TryPop(out var _);
			_list.Push(value);
		}

		public void Clear() {
			if(_list != null)
				_list.Clear();
		}

		public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
