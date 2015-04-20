using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

#if VELOX_DB
namespace Velox.DB.Core
#else
namespace Velox.Core
#endif
{
	public class ObjectMapper
	{
		public bool IncludePrivate { get; set; }
		public bool IncludeInherited { get; set; }
		public IEqualityComparer<string> Comparer { private get; set; }

		public ObjectMapper(bool includePrivate = false, bool includeInherited = false, bool ignoreCase = false)
		{
			IncludePrivate = includePrivate;
			IncludeInherited = includeInherited;
			IgnoreCase = ignoreCase;
		}

		public static ObjectMapper Mapper(bool includePrivate = false, bool includeInherited = false, bool ignoreCase = false)
		{
			return new ObjectMapper {
				IncludePrivate = includePrivate,
				IncludeInherited = includeInherited,
				IgnoreCase = ignoreCase
			};
		}

		public bool IgnoreCase
		{
			set 
            {
			    Comparer = value ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
			}
		}


		public void FillObject<T>(Func<string,Tuple<object,bool>> valueProvider)
		{
			FillObject(typeof(T), valueProvider);
		}

        public void FillObject(Type t, Func<string, Tuple<object, bool>> valueProvider)
        {
            FillObject((object)t, valueProvider);
        }

        public T CreateObject<T>(Func<string, Tuple<object, bool>> valueProvider) where T : new()
        {
            return FillObject(new T(), valueProvider);
        }

        public T FillObject<T>(T o, Func<string, Tuple<object, bool>> valueProvider)
        {
            return (T) FillObject((object)o, valueProvider);
        }

		public object FillObject(object o, Func<string,Tuple<object,bool>> valueProvider)
		{
            var type = ResolveObjectType(ref o);

			var fields = GetFields(type, o == null);

			HashSet<string> mappedNames = new HashSet<string>(Comparer);

			foreach (var field in fields)
			{
				if (mappedNames.Contains(field.Name))
					continue;

				var result = valueProvider(field.Name);

				if (result.Item2)
				{
					field.SetValue(o, result.Item1.Convert(field.FieldType));
					mappedNames.Add(field.Name);
				}
			}

		    return o;
		}


		public void FillObject<T>(Dictionary<string,object> values)
		{
			FillObject(typeof(T), values.Select(kvp => Tuple.Create(kvp.Key,kvp.Value)));
		}

        public void FillObject(Type t, Dictionary<string, object> values)
        {
            FillObject((object)t, values.Select(kvp => Tuple.Create(kvp.Key, kvp.Value)));
        }

        private object FillObject(object o, Dictionary<string, object> values)
        {
            return FillObject(o, values.Select(kvp => Tuple.Create(kvp.Key, kvp.Value)));
        }

        public T CreateObject<T>(Dictionary<string, object> values) where T:new()
        {
            return FillObject(new T(), values);
        }

        public T FillObject<T>(T o, Dictionary<string, object> values)
        {
            return (T) FillObject((object)o, values);
        }

        public void FillObject<T>(IEnumerable<Tuple<string, object>> values)
        {
            FillObject(typeof(T), values);
        }

        public T CreateObject<T>(IEnumerable<Tuple<string, object>> values) where T:new()
        {
            return FillObject(new T(), values);
        }

	    public T FillObject<T>(T o, IEnumerable<Tuple<string, object>> values)
	    {
	        return (T) FillObject((object) o, values);
	    }

		public object FillObject(object o, IEnumerable<Tuple<string,object>> values)
		{
			var type = ResolveObjectType(ref o);

			var fields = GetFields(type, o == null);

			foreach (var item in values)
			{
				var name = item.Item1;

				foreach (var field in fields)
					if (Comparer.Equals(name, field.Name))
					{
						field.SetValue(o, item.Item2.Convert(field.FieldType));
						break;
					}
			}

		    return o;
		}

		//---- Private helpers

		private Type ResolveObjectType(ref object o)
		{
			Type type = o.GetType();

			if (o is Type)
			{
				type = (Type)o;
				o = null;
			}

			return type;
		}

		private FieldOrPropertyInfo[] GetFields(Type type, bool staticMembers)
		{
			BindingFlags bindingFlags = BindingFlags.Public;

			if (!staticMembers)
				bindingFlags |= BindingFlags.Instance;
			else
				bindingFlags |= BindingFlags.Static;

			if (IncludePrivate)
				bindingFlags |= BindingFlags.NonPublic;

			if (!IncludeInherited)
				bindingFlags |= BindingFlags.DeclaredOnly;

			return type.Inspector().GetFieldsAndProperties(bindingFlags);
		}

	}
}

