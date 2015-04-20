using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;

#if VELOX_DB
namespace Velox.DB.Core
#else
namespace Velox.Core
#endif
{
    [Flags]
    public enum TypeFlags
    {
        Byte = 1<<0,
        SByte = 1<<1,
        Int16 = 1<<2,
        UInt16 = 1<<3,
        Int32 = 1<<4,
        UInt32 = 1<<5,
        Int64 = 1<<6,
        UInt64 = 1<<7,
        Single = 1<<8,
        Double = 1<<9,
        Decimal = 1<<10,
        Boolean = 1<<11,
        Char = 1<<12,
        Enum = 1<<13,
        DateTime = 1<<14,
        TimeSpan = 1<<15,
        DateTimeOffset = 1<<16,
        String = 1<<17,
        Guid = 1<<18,
        Nullable = 1<<24,
        ValueType = 1<<25,
        CanBeNull = 1<<26,
        Array = 1<<27,        
        Integer8 = Byte|SByte,
        Integer16 = Int16|UInt16,
        Integer32 = Int32|UInt32,
        Integer64 = Int64|UInt64,
        SignedInteger = SByte|Int16|Int32|Int64,
        UnsignedInteger = Byte|UInt16|UInt32|UInt64,
        FloatingPoint = Single|Double|Decimal,
        Integer = Integer8|Integer16|Integer32|Integer64,
        Numeric = Integer|FloatingPoint,
        Primitive = Integer|Boolean|Char|Single|Double
    }

    public class TypeInspector
    {
        private readonly Type _type;
        private readonly TypeInfo _typeInfo;
        private readonly Type _realType;
        private readonly TypeInfo _realTypeInfo;
        private readonly TypeFlags _typeFlags;

        private static readonly Dictionary<Type, TypeFlags> _typeflagsMap = new Dictionary<Type, TypeFlags>()
        {
            { typeof(Byte), TypeFlags.Byte},
            { typeof(SByte), TypeFlags.SByte},
            { typeof(Int16), TypeFlags.Int16},
            { typeof(UInt16), TypeFlags.UInt16},
            { typeof(Int32), TypeFlags.Int32},
            { typeof(UInt32), TypeFlags.UInt32},
            { typeof(Int64), TypeFlags.Int64},
            { typeof(UInt64), TypeFlags.UInt64},
            { typeof(Single), TypeFlags.Single},
            { typeof(Double), TypeFlags.Double},
            { typeof(Decimal), TypeFlags.Decimal},
            { typeof(Boolean), TypeFlags.Boolean},
            { typeof(Char), TypeFlags.Char},
            { typeof(DateTime), TypeFlags.DateTime},
            { typeof(TimeSpan), TypeFlags.TimeSpan},
            { typeof(DateTimeOffset), TypeFlags.DateTimeOffset},
            { typeof(String), TypeFlags.String},
            { typeof(Guid), TypeFlags.Guid}
        };


        public TypeInspector(Type type)
        {
            _type = type;
            _typeInfo = type.GetTypeInfo();
            _realType = Nullable.GetUnderlyingType(_type) ?? _type;
            _realTypeInfo = _realType.GetTypeInfo();

            _typeFlags = BuildTypeFlags();
        }

        private TypeFlags BuildTypeFlags()
        {
            TypeFlags flags;

            _typeflagsMap.TryGetValue(_realType, out flags);

            if (_type != _realType)
                flags |= TypeFlags.Nullable | TypeFlags.CanBeNull;

            if (_realTypeInfo.IsValueType)
                flags |= TypeFlags.ValueType;
            else
                flags |= TypeFlags.CanBeNull;

            if (_realTypeInfo.IsEnum)
            {
                TypeFlags enumTypeFlags;

                if (_typeflagsMap.TryGetValue(Enum.GetUnderlyingType(_realType), out enumTypeFlags))
                    flags |= enumTypeFlags;

                flags |= TypeFlags.Enum;
            }
            else if (_type.IsArray)
            {
                flags |= TypeFlags.Array;

                TypeFlags arrayTypeFlags;

                if (_typeflagsMap.TryGetValue(_type.GetElementType(), out arrayTypeFlags))
                    flags |= arrayTypeFlags;
            }

            return flags;
        }

        private T WalkAndFindSingle<T>(Func<Type, T> f)
        {
            Type t = _type;

            while (t != null)
            {
                T result = f(t);

                if (result != null)
                    return result;

                t = t.GetTypeInfo().BaseType;
            }

            return default(T);
        }

        private T[] WalkAndFindMultiple<T>(Func<Type, IEnumerable<T>> f) where T : class
        {
            var list = new List<T>();

            Type t = _type;

            while (t != null)
            {
                IEnumerable<T> result = f(t);

                if (result != null)
                    list.AddRange(result);

                t = t.GetTypeInfo().BaseType;
            }

            return list.ToArray();
        }

        public Type Type
        {
            get { return _type; }
        }

        public Type RealType
        {
            get { return _realType; }
        }

        public bool IsArray
        {
            get { return Is(TypeFlags.Array); }
        }

        public Type ArrayElementType
        {
            get { return IsArray ? _type.GetElementType() : null; }
        }

        public bool IsGenericType
        {
            get { return _typeInfo.IsGenericType; }
        }

        public bool IsGenericTypeDefinition
        {
            get { return _typeInfo.IsGenericTypeDefinition; }
        }

        public bool IsNullable
        {
            get { return Is(TypeFlags.Nullable); }
        }

        public bool CanBeNull
        {
            get { return Is(TypeFlags.CanBeNull); }
        }

        public bool IsPrimitive
        {
            get { return Is(TypeFlags.Primitive); }
        }

        public bool IsValueType
        {
            get { return Is(TypeFlags.ValueType); }
        }

        public Type BaseType
        {
            get { return _typeInfo.BaseType; }
        }

        public bool IsEnum
        {
            get { return Is(TypeFlags.Enum); }
        }

        public TypeFlags TypeFlags
        {
            get { return _typeFlags; }
        }

        public bool Is(TypeFlags flags)
        {
            return (TypeFlags & flags) != 0;
        }
  
        public bool IsSubclassOf(Type type)
        {
            return WalkAndFindSingle(t => t.GetTypeInfo().BaseType == type);
        }

        public object DefaultValue()
        {
            if (CanBeNull)
                return null;

            return Activator.CreateInstance(_realType);
        }

        public MethodInfo GetMethod(string name, Type[] types)
        {
            return WalkAndFindSingle(t => t.GetTypeInfo().GetDeclaredMethods(name).FirstOrDefault(mi => types.SequenceEqual(mi.GetParameters().Select(p => p.ParameterType))));
        }

        public MethodInfo GetMethod(string name, BindingFlags bindingFlags)
        {
            return WalkAndFindSingle(t => t.GetTypeInfo().GetDeclaredMethods(name).FirstOrDefault(mi => mi.Inspector().MatchBindingFlags(bindingFlags)));
        }

        public bool HasAttribute<T>(bool inherit) where T : Attribute
        {
            return _typeInfo.IsDefined(typeof(T), inherit);
        }

        public T GetAttribute<T>(bool inherit) where T : Attribute
        {
            return _typeInfo.GetCustomAttributes<T>(inherit).FirstOrDefault();
        }

        public T[] GetAttributes<T>(bool inherit) where T : Attribute
        {
            return (T[])_typeInfo.GetCustomAttributes(typeof(T), inherit).ToArray();
        }

        public bool IsAssignableFrom(Type type)
        {
            return _typeInfo.IsAssignableFrom(type.GetTypeInfo());
        }

        public ConstructorInfo[] GetConstructors()
        {
            return _typeInfo.DeclaredConstructors.ToArray();
        }

        public MemberInfo[] GetMember(string propertyName)
        {
            return WalkAndFindMultiple(t => t.GetTypeInfo().DeclaredMembers.Where(m => m.Name == propertyName));
        }

        public PropertyInfo GetIndexer(Type[] types)
        {
            return WalkAndFindSingle(t => t.GetTypeInfo().DeclaredProperties.FirstOrDefault(pi => pi.Name == "Item" && LazyBinder.MatchParameters(types, pi.GetIndexParameters())));
        }

        public T[] GetCustomAttributes<T>(bool inherit) where T:Attribute
        {
            return _typeInfo.GetCustomAttributes<T>(inherit).ToArray();
        }

        public MethodInfo GetPropertyGetter(string propertyName, Type[] parameterTypes)
        {
            return GetMethod("get_" + propertyName, parameterTypes);
        }

        public PropertyInfo GetProperty(string propName)
        {
            return WalkAndFindSingle(t => t.GetTypeInfo().GetDeclaredProperty(propName));
        }

        public FieldInfo GetField(string fieldName)
        {
            return WalkAndFindSingle(t => t.GetTypeInfo().GetDeclaredField(fieldName));
        }

        public bool ImplementsOrInherits(Type type)
        {
            if (type.GetTypeInfo().IsGenericTypeDefinition && type.GetTypeInfo().IsInterface)
            {
                return _typeInfo.ImplementedInterfaces.Any(t => (t.GetTypeInfo().IsGenericType && t.GetTypeInfo().GetGenericTypeDefinition() == type));
            }

            return type.GetTypeInfo().IsAssignableFrom(_typeInfo);
        }

        public bool ImplementsOrInherits<T>()
        {
            return ImplementsOrInherits(typeof (T));
        }

        public MethodInfo GetMethod(string methodName, BindingFlags bindingFlags, Type[] parameterTypes)
        {
            return WalkAndFindSingle(t => LazyBinder.SelectBestMethod(t.GetTypeInfo().GetDeclaredMethods(methodName),parameterTypes,bindingFlags));
        }

        public Type[] GetGenericArguments()
        {
            return _type.GenericTypeArguments;
        }


        public FieldInfo[] GetFields(BindingFlags bindingFlags)
        {
            if ((bindingFlags & BindingFlags.DeclaredOnly) != 0)
				return _typeInfo.DeclaredFields.Where(fi => fi.Inspector().MatchBindingFlags(bindingFlags)).ToArray();

            return WalkAndFindMultiple(t => t.GetTypeInfo().DeclaredFields.Where(fi => fi.Inspector().MatchBindingFlags(bindingFlags)));
        }

        public PropertyInfo[] GetProperties(BindingFlags bindingFlags)
        {
            if ((bindingFlags & BindingFlags.DeclaredOnly) != 0)
                return _typeInfo.DeclaredProperties.Where(pi => pi.Inspector().MatchBindingFlags(bindingFlags)).ToArray();

            return WalkAndFindMultiple(t => t.GetTypeInfo().DeclaredProperties.Where(pi => pi.Inspector().MatchBindingFlags(bindingFlags)));
        }

        public MethodInfo[] GetMethods(BindingFlags bindingFlags)
        {
            if ((bindingFlags & BindingFlags.DeclaredOnly) != 0)
                return _typeInfo.DeclaredMethods.Where(mi => mi.Inspector().MatchBindingFlags(bindingFlags)).ToArray();

            return WalkAndFindMultiple(t => t.GetTypeInfo().DeclaredMethods.Where(mi => mi.Inspector().MatchBindingFlags(bindingFlags)));
        }


        public Type[] GetInterfaces()
        {
            return _typeInfo.ImplementedInterfaces.ToArray();
        }

		public FieldOrPropertyInfo[] GetFieldsAndProperties(BindingFlags bindingFlags)
		{
			MemberInfo[] members;

			if ((bindingFlags & BindingFlags.DeclaredOnly) != 0)
                members = _typeInfo.DeclaredFields.Where(fi => fi.Inspector().MatchBindingFlags(bindingFlags)).Union<MemberInfo>(_typeInfo.DeclaredProperties.Where(pi => pi.Inspector().MatchBindingFlags(bindingFlags))).ToArray();
			else
                members = WalkAndFindMultiple(t => t.GetTypeInfo().DeclaredFields.Where(fi => fi.Inspector().MatchBindingFlags(bindingFlags)).Union<MemberInfo>(t.GetTypeInfo().DeclaredProperties.Where(pi => pi.Inspector().MatchBindingFlags(bindingFlags))));

			return members.Select(m => new FieldOrPropertyInfo(m)).ToArray();
		}
    }


}