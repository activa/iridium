#region License
//=============================================================================
// Iridium-Core - Portable .NET Productivity Library 
//
// Copyright (c) 2008-2016 Philippe Leybaert
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
using System.Linq;

#if IRIDIUM_CORE_EMBEDDED
namespace Iridium.DB.CoreUtil
#else
namespace Iridium.Core
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
					field.SetValue(o, result.Item1.Convert(field.Type));
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
						field.SetValue(o, item.Item2.Convert(field.Type));
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

