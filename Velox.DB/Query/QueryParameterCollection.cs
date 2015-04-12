#region License
//=============================================================================
// Velox.DB - Portable .NET ORM 
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
using Velox.DB.Core;


namespace Velox.DB
{
    public class QueryParameterCollection : IEnumerable<KeyValuePair<string,object>>
    {
        private readonly Dictionary<string, object> _dic;

        public QueryParameterCollection(IDictionary<string, object> parameters)
        {
            _dic = new Dictionary<string, object>(parameters);
        }

        public QueryParameterCollection(QueryParameterCollection parameters)
        {
            _dic = new Dictionary<string, object>(parameters.AsDictionary());
        }

        public QueryParameterCollection(object o = null)
        {
            _dic = new Dictionary<string, object>();

            AddParametersFromObject(o, true);
        }

        public void Merge(Dictionary<string, object> parameters, bool important = false)
        {
            foreach (var parameter in parameters.Where(parameter => important || !_dic.ContainsKey(parameter.Key)))
            {
                _dic[parameter.Key] = parameter.Value;
            }
        }

        public void Merge(QueryParameterCollection parameters, bool important = false)
        {
            if (parameters != null)
                Merge(parameters.AsDictionary(), important);
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

        public IEnumerable<string> Keys
        {
            get { return _dic.Keys; }
        }

        public Dictionary<string, object> AsDictionary()
        {
            return _dic;
        }

        private void AddParametersFromObject(object parameters, bool important)
        {
            if (parameters == null)
                return;

            var members = parameters.GetType().Inspector().GetFieldsAndProperties(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);

            foreach (var member in members.Where(member => important || !_dic.ContainsKey(member.Name)))
            {
                _dic[member.Name] = member.GetValue(parameters);
            }
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