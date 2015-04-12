#region License
//=============================================================================
// Velox.DB - Portable .NET ORM 
//
// Copyright (c) 2015 Philippe Leybaert
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

namespace Velox.DB.Sql
{
    public static class SqlNameGenerator
    {
        [ThreadStatic]
        private static int _currentTableAlias;
        [ThreadStatic]
        private static int _currentFieldAlias;
        [ThreadStatic]
        private static int _currentParamCounter;

        private static readonly string[] _tableAliases;

        static SqlNameGenerator()
        {
            var reservedWords = new HashSet<string>(new[] { "is", "as", "in", "on", "to", "at", "go", "by", "of", "or", "if", "no" });

            var aliasList = new List<string>();

            for (char c1 = 'a'; c1 < 'z'; c1++)
            {
                for (char c2 = 'a'; c2 < 'z'; c2++)
                {
                    var alias = new string(new[] { c1, c2 });

                    if (!reservedWords.Contains(alias))
                        aliasList.Add(alias);
                }
            }

            _tableAliases = aliasList.ToArray();
        }

        public static string NextTableAlias()
        {
            _currentTableAlias = _currentTableAlias%_tableAliases.Length;

            return _tableAliases[_currentTableAlias++];
        }

        public static string NextFieldAlias()
        {
            _currentFieldAlias = _currentFieldAlias%_tableAliases.Length;

            return "f" + _tableAliases[_currentFieldAlias++];
        }

        public static string NextParameterName()
        {
            string paramName = "P" + (++_currentParamCounter);

            if (_currentParamCounter >= 999)
                _currentParamCounter = 0;

            return paramName;
        }

        internal static void Reset()
        {
            _currentFieldAlias = 0;
            _currentTableAlias = 0;
            _currentParamCounter = 0;
        }
    }
}