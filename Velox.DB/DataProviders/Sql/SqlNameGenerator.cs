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