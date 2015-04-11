using System.Collections.Generic;
using System.Linq;
using Velox.DB.Core;

namespace Velox.DB
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

        public IEnumerable<string> FieldNames
        {
            get { return _dictionary.Keys; }
        }

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