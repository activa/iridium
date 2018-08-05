using System.Collections;
using System.Collections.Generic;

namespace Iridium.DB
{
    public class SafeDictionary<TK, TV> : IDictionary<TK, TV>
    {
        private readonly Dictionary<TK, TV> _dic;

        private IDictionary<TK,TV> AsInterface() => _dic;

        public SafeDictionary()
        {
            _dic = new Dictionary<TK, TV>();
        }

        public SafeDictionary(TV defaultValue) : this()
        {
            DefaultValue = defaultValue;
        }

        public SafeDictionary(IDictionary<TK, TV> dic)
        {
            _dic = new Dictionary<TK, TV>(dic);
        }

        public SafeDictionary(IDictionary<TK, TV> dic, TV defaultValue) : this(dic)
        {
            DefaultValue = defaultValue;
        }

        public SafeDictionary(IEqualityComparer<TK> comparer)
        {
            _dic = new Dictionary<TK, TV>(comparer);
        }

        public SafeDictionary(IEqualityComparer<TK> comparer, TV defaultValue) : this(comparer)
        {
            DefaultValue = defaultValue;
        }

        public SafeDictionary(IDictionary<TK,TV> dic, IEqualityComparer<TK> comparer)
        {
            _dic = new Dictionary<TK, TV>(dic,comparer);
        }

        public SafeDictionary(IDictionary<TK, TV> dic, IEqualityComparer<TK> comparer, TV defaultValue) : this(dic,comparer)
        {
            DefaultValue = defaultValue;
        }

        public void Add(KeyValuePair<TK, TV> item)
        {
            AsInterface().Add(item);
        }

        public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator()
        {
            return _dic.GetEnumerator();
        }

        public void Clear()
        {
            _dic.Clear();
        }

        public bool Contains(KeyValuePair<TK, TV> item)
        {
            return AsInterface().Contains(item);
        }

        public void CopyTo(KeyValuePair<TK, TV>[] array, int arrayIndex)
        {
            AsInterface().CopyTo(array,arrayIndex);
        }

        public bool Remove(KeyValuePair<TK, TV> item)
        {
            return AsInterface().Remove(item);
        }

        public int Count => _dic.Count;
        public bool IsReadOnly => AsInterface().IsReadOnly;
        public bool ContainsKey(TK key) => _dic.ContainsKey(key);

        public void Add(TK key, TV value)
        {
            _dic.Add(key,value);
        }

        public bool Remove(TK key)
        {
            return _dic.Remove(key);
        }

        public bool TryGetValue(TK key, out TV value)
        {
            return _dic.TryGetValue(key, out value);
        }

        public TV this[TK key]
        {
            get => ContainsKey(key) ? _dic[key] : DefaultValue;
            set => _dic[key] = value;
        }

        public ICollection<TK> Keys => _dic.Keys;
        public ICollection<TV> Values => _dic.Values;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TV DefaultValue { get; set; }
    }
}