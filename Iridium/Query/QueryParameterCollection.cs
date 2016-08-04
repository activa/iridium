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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Iridium.DB.CoreUtil;

namespace Iridium.DB
{
    public class QueryParameterCollection : IEnumerable<KeyValuePair<string,object>>
    {
        private readonly Dictionary<string, object> _dic;

        public QueryParameterCollection(object o = null)
        {
            _dic = new Dictionary<string, object>();

            AddParametersFromObject(o, true);
        }

        public void Merge(object parameters, bool important = false)
        {
            AddParametersFromObject(parameters, important);
        }

        public object this[string key]
        {
            get { return _dic[key]; }
            set { _dic[key] = value; }
        }

        public IEnumerable<string> Keys => _dic.Keys;

        public Dictionary<string, object> AsDictionary()
        {
            return _dic;
        }

        private IEnumerable<KeyValuePair<string, object>> EnumerateObject(object obj)
        {
            if (obj is IDictionary)
                return from object key in ((IDictionary) obj).Keys select new KeyValuePair<string, object>(key.ToString(), ((IDictionary) obj)[key]);
            
            if (obj is QueryParameterCollection)
                return ((QueryParameterCollection) obj).AsDictionary();

            var members = obj.GetType().Inspector().GetFieldsAndProperties(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);

            return members.Select(member => new KeyValuePair<string, object>(member.Name,member.GetValue(obj)));
        }

        private void AddParametersFromObject(object parameters, bool important)
        {
            if (parameters == null)
                return;

            foreach (var kv in EnumerateObject(parameters).Where(kv => important || !_dic.ContainsKey(kv.Key)))
                _dic[kv.Key] = kv.Value;
        }


        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _dic.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}