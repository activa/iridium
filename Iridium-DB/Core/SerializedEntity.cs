#region License
//=============================================================================
// Iridium - Porable .NET ORM 
//
// Copyright (c) 2015 Philippe Leybaert
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

using System.Collections.Generic;
using Iridium.Core;

namespace Iridium.DB
{
    public class SerializedEntity
    {
        readonly Dictionary<string, object> _dictionary;

        public SerializedEntity(Dictionary<string, object> dictionary)
        {
            _dictionary = new Dictionary<string, object>(dictionary);
        }

        public bool Contains(string field)
        {
            return _dictionary.ContainsKey(field);
        }

        public IEnumerable<string> FieldNames => _dictionary.Keys;

        public object this[string fieldName]
        {
            get { return _dictionary[fieldName]; }
            set { _dictionary[fieldName] = value; }
        }

        public Dictionary<string, object> AsDictionary()
        {
            return new Dictionary<string, object>(_dictionary);
        }

        public T CreateObject<T>() where T : new()
        {
            return ObjectMapper.Mapper(includeInherited: true).CreateObject<T>(_dictionary);
        }
    }
}