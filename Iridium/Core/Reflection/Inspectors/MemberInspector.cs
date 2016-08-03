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
using System.Linq;
using System.Reflection;

#if IRIDIUM_CORE_EMBEDDED
namespace Iridium.DB.Core
#else
namespace Iridium.Core
#endif
{
    public class MemberInspector
    {
        private readonly MemberInfo _memberInfo;

        public MemberInspector(MemberInfo memberInfo)
        {
            _memberInfo = memberInfo;
        }

        public bool HasAttribute(Type attributeType, bool inherit = false)
        {
            return _memberInfo.IsDefined(attributeType, inherit);
        }

        public bool HasAttribute<T>(bool inherit = false) where T : Attribute
        {
            return _memberInfo.IsDefined(typeof(T), inherit);
        }

        public Attribute[] GetAttributes(Type attributeType, bool inherit = false)
        {
            return (Attribute[]) _memberInfo.GetCustomAttributes(attributeType, inherit);
        }

        public Attribute GetAttribute(Type attributeType, bool inherit = false)
        {
            return GetAttributes(attributeType, inherit).FirstOrDefault();
        }

        public T GetAttribute<T>(bool inherit = false) where T : Attribute
        {
            return (T) GetAttribute(typeof (T), inherit);
        }

        public T[] GetAttributes<T>(bool inherit = false) where T : Attribute
        {
            return (T[])GetAttributes(typeof(T), inherit);
        }

        public bool IsStatic
        {
            get
            {
                if (_memberInfo is PropertyInfo)
                    return ((PropertyInfo) _memberInfo).GetMethod.IsStatic;
                if (_memberInfo is FieldInfo)
                    return ((FieldInfo) _memberInfo).IsStatic;
                if (_memberInfo is MethodBase)
                    return ((MethodBase) _memberInfo).IsStatic;
                return false;
            }
        }

        public bool IsPublic
        {
            get
            {
                if (_memberInfo is PropertyInfo)
                    return ((PropertyInfo) _memberInfo).GetMethod.IsPublic;
                if (_memberInfo is FieldInfo)
                    return ((FieldInfo) _memberInfo).IsPublic;
                if (_memberInfo is MethodBase)
                    return ((MethodBase) _memberInfo).IsPublic;
                return false;
            }
        }

        public bool IsWritePublic
        {
            get
            {
                if (_memberInfo is PropertyInfo)
                {
                    var propertyInfo = ((PropertyInfo) _memberInfo);

                    return propertyInfo.SetMethod != null && propertyInfo.SetMethod.IsPublic;
                }

                return IsPublic;
            }
            
        }

        internal bool MatchBindingFlags(BindingFlags flags)
        {
            if (flags == BindingFlags.Default)
                return true;

            if (IsStatic && (flags & BindingFlags.Static) == 0)
                return false;

            if (!IsStatic && (flags & BindingFlags.Instance) == 0)
                return false;

            if (IsPublic && (flags & BindingFlags.Public) == 0)
                return false;

            if (!IsPublic && (flags & BindingFlags.NonPublic) == 0)
                return false;

            return true;
        }

        public object GetValue(object instance)
        {
            if (_memberInfo is PropertyInfo)
                return ((PropertyInfo) _memberInfo).GetValue(instance);
            if (_memberInfo is FieldInfo)
                return ((FieldInfo)_memberInfo).GetValue(instance);

            throw new InvalidOperationException();
        }

        public void SetValue(object instance, object value)
        {
            if (_memberInfo is PropertyInfo)
                ((PropertyInfo)_memberInfo).SetValue(instance, value);
            else if (_memberInfo is FieldInfo)
                ((FieldInfo)_memberInfo).SetValue(instance, value);
            else
                throw new InvalidOperationException();
        }


    }
}