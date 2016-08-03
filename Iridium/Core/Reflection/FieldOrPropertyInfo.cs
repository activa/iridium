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
using System.Reflection;

#if IRIDIUM_CORE_EMBEDDED
namespace Iridium.DB.Core
#else
namespace Iridium.Core
#endif
{
    public class FieldOrPropertyInfo
    {
        private readonly MemberInfo _memberInfo;
        public readonly Type Type;
        private TypeInspector _typeInspector;
        private MemberInspector _memberInspector;

        public FieldOrPropertyInfo(MemberInfo memberInfo)
        {
            _memberInfo = memberInfo;

            Type = ((_memberInfo is FieldInfo) ? ((FieldInfo)_memberInfo).FieldType : ((PropertyInfo)_memberInfo).PropertyType);
        }

        public MemberInspector Inspector => (_memberInspector ?? (_memberInspector = _memberInfo.Inspector()));

        public TypeInspector TypeInspector => (_typeInspector ?? (_typeInspector = Type.Inspector()));

        public string Name => _memberInfo.Name;

        public object GetValue(object o)
        {
            return _memberInfo is FieldInfo ? ((FieldInfo)_memberInfo).GetValue(o) : ((PropertyInfo)_memberInfo).GetValue(o, null);
        }

        public void SetValue(object o, object value)
        {
            if (_memberInfo is FieldInfo)
                ((FieldInfo) _memberInfo).SetValue(o, value);
            else
                ((PropertyInfo)_memberInfo).SetValue(o, value, null);
        }

		public bool IsField => _memberInfo is FieldInfo;
        public bool IsProperty => _memberInfo is PropertyInfo;

        public FieldInfo AsField => _memberInfo as FieldInfo;
        public PropertyInfo AsProperty => _memberInfo as PropertyInfo;
        public MemberInfo AsMember => _memberInfo;

        public Action<object,object> Setter() => SetValue;

        public Action<object> Setter(object target) => value => SetValue(target, value);

        public Action<object,T> Setter<T>() => (target, value) => SetValue(target, value);

        public Action<T> Setter<T>(object target) => value => SetValue(target, value);

        public Func<object,object> Getter() => GetValue;

        public Func<object,T> Getter<T>() => target => (T) GetValue(target);

        public Func<object> Getter(object target) => () => GetValue(target);

        public Func<T> Getter<T>(object target) => () => (T) GetValue(target);

        public bool IsPrivate
		{
			get 
			{ 
				if (IsField)
					return AsField.IsPrivate;

				var method = AsProperty.GetMethod ?? AsProperty.SetMethod;

				return method.IsPrivate;
			}
		}

		public bool IsStatic
		{
			get
			{
				if (IsField)
					return AsField.IsStatic;

				var method = AsProperty.GetMethod ?? AsProperty.SetMethod;

				return method.IsStatic;
			}
		}

        public Attribute[] GetCustomAttributes(Type type, bool inherit) => _memberInfo.Inspector().GetAttributes(type, inherit);

        public T[] GetCustomAttributes<T>(bool inherit) where T:Attribute => _memberInfo.Inspector().GetAttributes<T>(inherit);

        public bool IsDefined(Type type, bool b) => _memberInfo.IsDefined(type, b);
    }
}