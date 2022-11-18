using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Iridium.DB
{
    public class ReadOnlySet<T> : IReadOnlyCollection<T>
    {
        private HashSet<T> _set;

        public ReadOnlySet(IEnumerable<T> items)
        {
            if (items == null)
                _set = new HashSet<T>();
            else
                _set = new HashSet<T>(items);
        }

        public ReadOnlySet<T> Union(IEnumerable<T> items)
        {
            var newSet = new ReadOnlySet<T>(_set);
            newSet._set.UnionWith(items);
            return newSet;
        }

        public ReadOnlySet<T> Intersection(IEnumerable<T> items)
        {
            var newSet = new ReadOnlySet<T>(_set);
            newSet._set.IntersectWith(items);
            return newSet;
        }

        public bool Contains(T item)
        {
            return _set.Contains(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count { get; }
    }
}