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
namespace Iridium.DB.CoreUtil
#else
namespace Iridium.Core
#endif
{
    public class AssemblyInspector
    {
        private readonly Assembly _assembly;

        public AssemblyInspector(Assembly assembly)
        {
            _assembly = assembly;
        }

        /// <summary>
        /// Finds all types derived from the given type, limiting the search to the given assembly
        /// </summary>
        /// <param name="baseType">The base type or interface to use for finding types</param>
        /// <returns>An array of all types found in the given assembly which are either derived from the given type, or implement the given interface</returns>
        public Type[] FindCompatibleTypes(Type baseType)
        {
            TypeInfo baseTypeInfo = baseType.GetTypeInfo();

            return _assembly.DefinedTypes
                                .Where(type => type != baseTypeInfo && baseTypeInfo.IsAssignableFrom(type))
                                .Select(typeInfo => typeInfo.AsType())
                                .ToArray();
        }

        #region Method description
        /// <summary>
        /// Finds all types derived from the given type, limiting the search to the given assembly
        /// </summary>
        /// <returns>An array of all types found in the given assembly which are either derived from the given type, or implement the given interface</returns>
        #endregion
        public Type[] FindCompatibleTypes<T>()
        {
            return FindCompatibleTypes(typeof(T));
        }

    }
}