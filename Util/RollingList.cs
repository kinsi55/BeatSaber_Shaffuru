using System;
using System.Collections;
using System.Collections.Generic;

namespace Shaffuru.Util {
	// Ring buffer which drops the oldest item once full. Optionally keeps a map of its items so Contains() is O(1)
	class RollingList<T> : IEnumerable<T> {
		readonly object _lock = new object();
		readonly Dictionary<T, int> _counts;

		T[] _items = Array.Empty<T>();
		// Index of the oldest item
		int _start = 0;

		public RollingList(int maximumCount, bool mapped = false) {
			if(mapped)
				_counts = new Dictionary<T, int>();

			SetSize(maximumCount);
		}

		public int maximumCount => _items.Length;
		public int Count { get; private set; }

		public void SetSize(int maximumCount) {
			lock(_lock) {
				if(maximumCount < 0)
					maximumCount = 0;

				// Drop the oldest items that dont fit anymore
				while(Count > maximumCount)
					RemoveOldest();

				var newItems = new T[maximumCount];
				for(var i = 0; i < Count; i++)
					newItems[i] = _items[(_start + i) % _items.Length];

				_items = newItems;
				_start = 0;
			}
		}

		public void Add(T value) {
			lock(_lock) {
				if(_items.Length == 0)
					return;

				if(Count == _items.Length)
					RemoveOldest();

				_items[(_start + Count) % _items.Length] = value;
				Count++;

				if(_counts != null)
					_counts[value] = _counts.TryGetValue(value, out var c) ? c + 1 : 1;
			}
		}

		void RemoveOldest() {
			var oldest = _items[_start];

			_items[_start] = default;
			_start = (_start + 1) % _items.Length;
			Count--;

			if(_counts != null) {
				if(_counts[oldest] == 1) {
					_counts.Remove(oldest);
				} else {
					_counts[oldest]--;
				}
			}
		}

		public bool Contains(T value) {
			lock(_lock) {
				if(_counts != null)
					return _counts.ContainsKey(value);

				var comparer = EqualityComparer<T>.Default;
				for(var i = 0; i < Count; i++) {
					if(comparer.Equals(_items[(_start + i) % _items.Length], value))
						return true;
				}
				return false;
			}
		}

		public void Clear() {
			lock(_lock) {
				Array.Clear(_items, 0, _items.Length);
				_start = 0;
				Count = 0;
				_counts?.Clear();
			}
		}

		public IEnumerator<T> GetEnumerator() {
			T[] snapshot;
			lock(_lock) {
				snapshot = new T[Count];
				for(var i = 0; i < Count; i++)
					snapshot[i] = _items[(_start + i) % _items.Length];
			}
			return ((IEnumerable<T>)snapshot).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
