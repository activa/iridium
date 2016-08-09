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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Iridium.DB.CoreUtil;

namespace Iridium.DB
{
    public class QueryParameter
    {
        public QueryParameter(string name, object value, Type type = null)
        {
            Name = name;
            Value = value;
            Type = (type ?? value?.GetType()) ?? typeof(object);
        }

        public readonly string Name;
        public readonly object Value;
        public readonly Type Type;
    }

    public class QueryParameterCollection : IEnumerable<QueryParameter>
    {
        private readonly Dictionary<string, QueryParameter> _dic;

        public QueryParameterCollection()
        {
            _dic = new Dictionary<string, QueryParameter>();
            
        }

        public QueryParameterCollection(IEnumerable<QueryParameter> parameters)
        {
            _dic = parameters.ToDictionary(p => p.Name, p => p);
        }

        private QueryParameterCollection(object o = null)
        {
            _dic = new Dictionary<string, QueryParameter>();

            AddParametersFromObject(o, true);
        }

        public static QueryParameterCollection FromObject(object o = null)
        {
            return new QueryParameterCollection(o);
        }

        public void Merge(object parameters, bool important = false)
        {
            AddParametersFromObject(parameters, important);
        }

        public QueryParameter this[string key]
        {
            get { return _dic[key]; }
            set { _dic[key] = value; }
        }

        public IEnumerable<string> Keys => _dic.Keys;

        private IEnumerable<QueryParameter> EnumerateObject(object obj)
        {
            if (obj is IDictionary)
            {
                var dictionary = ((IDictionary) obj);

                return from object key in dictionary.Keys select new QueryParameter(key.ToString(), dictionary[key]);
            }

            if (obj is QueryParameterCollection)
                return ((QueryParameterCollection) obj);

            var members = obj.GetType().Inspector().GetFieldsAndProperties(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);

            return members.Select(member => new QueryParameter(member.Name,member.GetValue(obj), member.Type));
        }

        private void AddParametersFromObject(object parameters, bool important)
        {
            if (parameters == null)
                return;

            foreach (var param in EnumerateObject(parameters).Where(p => important || !_dic.ContainsKey(p.Name)))
                _dic[param.Name] = param;
        }


        public IEnumerator< QueryParameter> GetEnumerator()
        {
            return _dic.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}