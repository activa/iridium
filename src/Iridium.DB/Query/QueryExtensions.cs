#region License
//=============================================================================
// Iridium - Porable .NET ORM 
//
// Copyright (c) 2015-2017 Philippe Leybaert
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

namespace Iridium.DB
{
    public static class QueryExtensions
    {
        // IsAnyOf()

        public static bool IsAnyOf(this string value, params string[] values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this string value, IEnumerable<string> values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this int value, params int[] values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this int value, IEnumerable<int> values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this int? value, IEnumerable<int?> values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this int? value, params int?[] values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this long value, params long[] values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this long value, IEnumerable<long> values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this long? value, params long?[] values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this long? value, IEnumerable<long?> values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this decimal value, params decimal[] values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this decimal value, IEnumerable<decimal> values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this decimal? value, params decimal?[] values)
        {
            return values.Contains(value);
        }

        public static bool IsAnyOf(this decimal? value, IEnumerable<decimal?> values)
        {
            return values.Contains(value);
        }


        // IsNotAnyOf()

        public static bool IsNotAnyOf(this string value, params string[] values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this string value, IEnumerable<string> values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this int value, params int[] values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this int value, IEnumerable<int> values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this int? value, IEnumerable<int?> values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this int? value, params int?[] values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this long value, params long[] values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this long value, IEnumerable<long> values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this long? value, params long?[] values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this long? value, IEnumerable<long?> values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this decimal value, params decimal[] values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this decimal value, IEnumerable<decimal> values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this decimal? value, params decimal?[] values)
        {
            return !value.IsAnyOf(values);
        }

        public static bool IsNotAnyOf(this decimal? value, IEnumerable<decimal?> values)
        {
            return !value.IsAnyOf(values);
        }


        // IsBetween()

        public static bool IsBetween(this int value, int from, int to)
        {
            return value >= from && value <= to;
        }

        public static bool IsBetween(this int? value, int from, int to)
        {
            if (value == null)
                return false;

            return value >= from && value <= to;
        }

        public static bool IsBetween(this long value, long from, long to)
        {
            return value >= from && value <= to;
        }

        public static bool IsBetween(this long? value, long from, long to)
        {
            if (value == null)
                return false;

            return value >= from && value <= to;
        }

        public static bool IsBetween(this decimal value, decimal from, decimal to)
        {
            return value >= from && value <= to;
        }

        public static bool IsBetween(this decimal? value, decimal from, decimal to)
        {
            if (value == null)
                return false;

            return value >= from && value <= to;
        }

        public static bool IsBetween(this double value, double from, double to)
        {
            return value >= from && value <= to;
        }

        public static bool IsBetween(this double? value, double from, double to)
        {
            if (value == null)
                return false;

            return value >= from && value <= to;
        }

        public static bool IsBetween(this DateTime value, DateTime from, DateTime to)
        {
            return value >= from && value <= to;
        }

        public static bool IsBetween(this DateTime? value, DateTime from, DateTime to)
        {
            if (value == null)
                return false;

            return value >= from && value <= to;
        }

        public static bool IsBetween(this string value, string from, string to)
        {
            return (string.CompareOrdinal(value,from) >= 0 && string.CompareOrdinal(value,to) <= 0);
        }


        // IsNotBetween()

        public static bool IsNotBetween(this int value, int from, int to)
        {
            return !value.IsBetween(from, to);
        }

        public static bool IsNotBetween(this int? value, int from, int to)
        {
            return !value.IsBetween(from, to);
        }

        public static bool IsNotBetween(this long value, long from, long to)
        {
            return !value.IsBetween(from, to);
        }

        public static bool IsNotBetween(this long? value, long from, long to)
        {
            return !value.IsBetween(from, to);
        }

        public static bool IsNotBetween(this decimal value, decimal from, decimal to)
        {
            return !value.IsBetween(from, to);
        }

        public static bool IsNotBetween(this decimal? value, decimal from, decimal to)
        {
            return !value.IsBetween(from, to);
        }

        public static bool IsNotBetween(this double value, double from, double to)
        {
            return !value.IsBetween(from, to);
        }

        public static bool IsNotBetween(this double? value, double from, double to)
        {
            return !value.IsBetween(from, to);
        }

        public static bool IsNotBetween(this DateTime value, DateTime from, DateTime to)
        {
            return !value.IsBetween(from, to);
        }

        public static bool IsNotBetween(this DateTime? value, DateTime from, DateTime to)
        {
            return !value.IsBetween(from, to);
        }

        public static bool IsNotBetween(this string value, string from, string to)
        {
            return !value.IsBetween(from, to);
        }
    }
}