using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaffuru.Util {
    //https://stackoverflow.com/questions/14702654/rolling-list-in-net/18404024#18404024
    class RollingList<T> : IEnumerable<T> {
        private readonly List<T> _list = new List<T>();

        public RollingList(int maximumCount) {
            SetSize(maximumCount);
        }

        public void SetSize(int maximumCount) {
            MaximumCount = maximumCount;

            if(_list.Count > MaximumCount)
                _list.RemoveRange(0, _list.Count - MaximumCount);
        }

        public int MaximumCount { get; private set; }
        public int Count => _list.Count;

        public void Add(T value) {
            if(_list.Count == MaximumCount && _list.Count > 0)
                _list.RemoveAt(0);
            _list.Add(value);
        }

        public T this[int index] {
            get {
                if(index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException();

                return _list[index];
            }
        }

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
