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
using System.Reflection;
using System.Threading;

#if IRIDIUM_CORE_EMBEDDED
namespace Iridium.DB.CoreUtil
#else
namespace Iridium.Core
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

	    public static AssemblyInspector Inspector(this Assembly assembly)
	    {
	        return new AssemblyInspector(assembly);
	    }




    }
}