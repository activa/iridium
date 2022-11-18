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
using System.Collections.Generic;
using System.Threading;
using Iridium.Reflection;

namespace Iridium.DB
{
    public class SerializedEntity
    {
        private static readonly ThreadLocal<Dictionary<Type,MemberInspector[]>> _membersCache = new(() => new Dictionary<Type, MemberInspector[]>());

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
        }
    }
}