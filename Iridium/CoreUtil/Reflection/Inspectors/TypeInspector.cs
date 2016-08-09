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
using System.Reflection;

#if IRIDIUM_CORE_EMBEDDED
namespace Iridium.DB.CoreUtil
#else
namespace Iridium.Core
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
        Array = 1 << 21,
        Nullable = 1<<24,
        ElementNullable = 1 << 25,
        ValueType = 1<<26,
        ElementValueType = 1 << 27,
        CanBeNull = 1<<28,
        ElementCanBeNull = 1 << 29,
        Integer8 = Byte|SByte,
        Integer16 = Int16|UInt16|Char,
        Integer32 = Int32|UInt32,
        Integer64 = Int64|UInt64,
        SignedInteger = Char|SByte|Int16|Int32|Int64,
        UnsignedInteger = Byte|UInt16|UInt32|UInt64,
        FloatingPoint = Single|Double|Decimal,
        Integer = Integer8|Integer16|Integer32|Integer64,
        Numeric = Integer|FloatingPoint,
        Primitive = Integer|Boolean|Char|Single|Double
    }

    public class TypeInspector
    {
        private readonly TypeInfo _typeInfo;
        private readonly TypeInfo _realTypeInfo;
        private readonly TypeInspector _elementTypeInspector;

        public Type Type { get; }
        public Type RealType { get; }
        public TypeFlags TypeFlags { get; }

        private const TypeFlags TypeDesignator = (TypeFlags) ((1 << 19) - 1);
        private const TypeFlags TypeModifier = TypeFlags.CanBeNull|TypeFlags.Nullable|TypeFlags.ValueType| TypeFlags.ElementCanBeNull|TypeFlags.ElementNullable|TypeFlags.ElementValueType;

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
            Type = type;
            RealType = Nullable.GetUnderlyingType(Type) ?? Type;

            _typeInfo = type.GetTypeInfo();
            _realTypeInfo = RealType.GetTypeInfo();

            if (type.IsArray)
                _elementTypeInspector = type.GetElementType().Inspector();

            TypeFlags = BuildTypeFlags();
        }

        private TypeFlags BuildTypeFlags()
        {
            TypeFlags flags;

            _typeflagsMap.TryGetValue(RealType, out flags);

            if (Type != RealType)
                flags |= TypeFlags.Nullable | TypeFlags.CanBeNull;

            if (_realTypeInfo.IsValueType)
                flags |= TypeFlags.ValueType;
            else
                flags |= TypeFlags.CanBeNull;

            if (_realTypeInfo.IsEnum)
            {
                TypeFlags enumTypeFlags;

                if (_typeflagsMap.TryGetValue(Enum.GetUnderlyingType(RealType), out enumTypeFlags))
                    flags |= enumTypeFlags;

                flags |= TypeFlags.Enum;
            }
            else if (Type.IsArray)
            {
                flags |= TypeFlags.Array;

                TypeFlags arrayTypeFlags;

                if (_typeflagsMap.TryGetValue(Type.GetElementType(), out arrayTypeFlags))
                    flags |= arrayTypeFlags;

                if ((_elementTypeInspector.TypeFlags & TypeFlags.CanBeNull) != 0)
                    flags |= TypeFlags.ElementCanBeNull;
                if ((_elementTypeInspector.TypeFlags & TypeFlags.Nullable) != 0)
                    flags |= TypeFlags.ElementNullable;
                if ((_elementTypeInspector.TypeFlags & TypeFlags.ValueType) != 0)
                    flags |= TypeFlags.ElementValueType;
            }

            return flags;
        }

        private T WalkAndFindSingle<T>(Func<Type, T> f)
        {
            Type t = Type;

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

            Type t = Type;

            while (t != null)
            {
                IEnumerable<T> result = f(t);

                if (result != null)
                    list.AddRange(result);

                t = t.GetTypeInfo().BaseType;
            }

            return list.ToArray();
        }


        public bool IsArray => Is(TypeFlags.Array);
        public Type ArrayElementType => IsArray ? Type.GetElementType() : null;
        public bool IsGenericType => _typeInfo.IsGenericType;
        public bool IsGenericTypeDefinition => _typeInfo.IsGenericTypeDefinition;
        public bool IsNullable => Is(TypeFlags.Nullable);
        public bool CanBeNull => Is(TypeFlags.CanBeNull);
        public bool IsPrimitive => Is(TypeFlags.Primitive);
        public bool IsValueType => Is(TypeFlags.ValueType);
        public Type BaseType => _typeInfo.BaseType;
        public bool IsEnum => Is(TypeFlags.Enum);

        public bool Is(TypeFlags flags)
        {
            return (
                   ((flags & TypeDesignator) == 0 || (TypeFlags & flags & TypeDesignator) != 0)  
                && (((flags & (TypeDesignator|TypeFlags.Array)) == 0) || (flags & TypeFlags.Array) == (TypeFlags & TypeFlags.Array))
                && (flags & TypeFlags & TypeModifier) == (flags & TypeModifier)
                );
        }

        public TypeInspector ElementType => _elementTypeInspector;

        public bool IsSubclassOf(Type type) => WalkAndFindSingle(t => t.GetTypeInfo().BaseType == type);

        public object DefaultValue()
        {
            if (CanBeNull)
                return null;

            return Activator.CreateInstance(RealType);
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
            return WalkAndFindSingle(t => t.GetTypeInfo().DeclaredProperties.FirstOrDefault(pi => pi.Name == "Item" && SmartBinder.MatchParameters(types, pi.GetIndexParameters())));
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
            return WalkAndFindSingle(t => SmartBinder.SelectBestMethod(t.GetTypeInfo().GetDeclaredMethods(methodName),parameterTypes,bindingFlags));
        }

        public Type[] GetGenericArguments() => Type.GenericTypeArguments;

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

        public Type[] GetInterfaces() => _typeInfo.ImplementedInterfaces.ToArray();

        public FieldOrPropertyInfo[] GetFieldsAndProperties(BindingFlags bindingFlags)
		{
			MemberInfo[] members;

			if ((bindingFlags & BindingFlags.DeclaredOnly) != 0)
                members = _typeInfo.DeclaredFields.Where(fi => fi.Inspector().MatchBindingFlags(bindingFlags)).Union<MemberInfo>(_typeInfo.DeclaredProperties.Where(pi => pi.Inspector().MatchBindingFlags(bindingFlags))).ToArray();
			else
                members = WalkAndFindMultiple(t => t.GetTypeInfo().DeclaredFields.Where(fi => fi.Inspector().MatchBindingFlags(bindingFlags)).Union<MemberInfo>(t.GetTypeInfo().DeclaredProperties.Where(pi => pi.Inspector().MatchBindingFlags(bindingFlags))));

			return members.Select(m => new FieldOrPropertyInfo(m)).ToArray();
		}

        public Func<object, object> ImplicitConversion(Type fromType)
        {
            var implicitOperator = GetMethod("op_Implicit", new[] { fromType });

            if (implicitOperator != null)
                return o => implicitOperator.Invoke(null, new[] { o });

            return null;
        }

        public object Cast(object value)
        {
            var conversion = ImplicitConversion(value.GetType());

            if (conversion != null)
                return conversion(value);

            if (Is(TypeFlags.Numeric) && value is char)
                value = (short)(char)value; // compiler supports char to number casting but framework does not

            return Convert.ChangeType(value, Type, null);
        }
    }


}