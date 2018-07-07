#region License
//=============================================================================
// Iridium - Porable .NET ORM 
//
// Copyright (c) 2015-2017 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Iridium.Reflection;

namespace Iridium.DB
{
    public class SerializedEntity
    {
        private static ThreadLocal<Dictionary<Type,MemberInspector[]>> _membersCache = new ThreadLocal<Dictionary<Type, MemberInspector[]>>(() => new Dictionary<Type, MemberInspector[]>());

        private readonly Dictionary<string, object> _dic;

        public SerializedEntity(Dictionary<string, object> dictionary)
        {
            _dic = new Dictionary<string, object>(dictionary);
        }

        public bool Contains(string field)
        {
            return _dic.ContainsKey(field);
        }

        public IEnumerable<string> FieldNames => _dic.Keys;

        public object this[string fieldName]
        {
            get => _dic[fieldName];
            set => _dic[fieldName] = value;
        }

        public Dictionary<string, object> AsDictionary()
        {
            return new Dictionary<string, object>(_dic);
        }

        public T CreateObject<T>() where T : new()
        {
            var obj = new T();

            if (!_membersCache.Value.TryGetValue(typeof(T), out var fields))
            {
                fields = typeof(T).Inspector().GetFieldsAndProperties(BindingFlags.Instance | BindingFlags.Public);

                _membersCache.Value.Add(typeof(T), fields);
            }

            foreach (var field in fields)
            {
                if (_dic.TryGetValue(field.Name, out var value))
                {
                    field.SetValue(obj, value.Convert(field.Type));
                }
            }

            return obj;

            //return ObjectMapper.Mapper(includeInherited: true).CreateObject<T>(_dic);
        }
    }

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