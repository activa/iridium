using System.Collections.Generic;
using System;
using System.Reflection;
using System.Threading;

#if VELOX_DB
namespace Velox.DB.Core
#else
namespace Velox.Core
#endif
{
	public static class ReflectionExtensions
	{
        private static readonly ThreadLocal<Dictionary<Type,TypeInspector>> _typeInspectorCache = new ThreadLocal<Dictionary<Type, TypeInspector>>(() => new Dictionary<Type, TypeInspector>());
       
        public static TypeInspector Inspector(this Type type)
        {
            TypeInspector inspector;

            if (!_typeInspectorCache.Value.TryGetValue(type, out inspector))
            {
                inspector = new TypeInspector(type);

                _typeInspectorCache.Value[type] = inspector;
            }

            return inspector;
        }

	    public static MemberInspector Inspector(this MemberInfo memberInfo)
	    {
	        return new MemberInspector(memberInfo);
	    }

	    public static PropertyInspector Inspector(this PropertyInfo propertyInfo)
	    {
	        return new PropertyInspector(propertyInfo);
	    }
    }
}