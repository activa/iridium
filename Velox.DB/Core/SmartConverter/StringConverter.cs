#region License
//=============================================================================
// VeloxDB Core - Portable .NET Productivity Library 
//
// Copyright (c) 2008-2015 Philippe Leybaert
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
using System.Collections.Generic;
using System.Globalization;

#if VELOX_DB
namespace Velox.DB.Core
#else
namespace Velox.Core
#endif
{
    public static class StringConverter
    {
        private class TypedStringConverter<T> : IStringConverter
        {
            private readonly IStringConverter<T> _converter;

            public TypedStringConverter(IStringConverter<T> converter)
            {
                _converter = converter;
            }

            public bool TryConvert(string s, Type targetType, out object value)
            {
                value = null;

                if (!typeof(T).Inspector().IsAssignableFrom(targetType))
                    return false;

                T typedValue;

                if (_converter.TryConvert(s, out typedValue))
                {
                    value = typedValue;
                    return true;
                }

                return false;
                
            }
        }

        private static List<IStringConverter> _stringConverters;

        private static readonly object _staticLock = new object();
        private static string[] _dateFormats = new[] { "yyyyMMdd", "yyyy-MM-dd", "yyyy.MM.dd", "yyyy/MM/dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss" };

        public static void UnregisterAllStringConverters()
        {
            lock (_staticLock)
            {
                _stringConverters = null;
            }
            
        }

        public static void UnregisterStringConverter(IStringConverter stringConverter)
        {
            lock (_staticLock)
            {
                if (_stringConverters != null)
                    _stringConverters.Remove(stringConverter);
            }
        }

        public static void RegisterStringConverter(IStringConverter stringConverter)
        {
            lock (_staticLock)
            {
                _stringConverters = _stringConverters ?? new List<IStringConverter>();

                _stringConverters.Add(stringConverter);
            }
        }

        public static void RegisterStringConverter<T>(IStringConverter<T> stringConverter)
        {
            RegisterStringConverter(new TypedStringConverter<T>(stringConverter));
        }

        public static void RegisterDateFormats(params string[] dateFormats)
        {
            _dateFormats = dateFormats;
        }

        public static void RegisterDateFormat(string dateFormat)
        {
            RegisterDateFormat(dateFormat,false);
            
        }

        public static void RegisterDateFormat(string dateFormat, bool replace)
        {
            if (replace)
            {
                _dateFormats = new[] {dateFormat};
            }
            else
            {
                _dateFormats = _dateFormats.Union(new[] {dateFormat}).ToArray();
            }
        }

        public static T To<T>(this string stringValue, params string[] dateFormats)
        {
            return Convert<T>(stringValue, dateFormats);
        }

        public static object To(this string stringValue, Type targetType, params string[] dateFormats)
        {
            return Convert(stringValue, targetType, dateFormats);
        }

        public static T Convert<T>(this string stringValue, params string[] dateFormats)
        {
            return (T) Convert(stringValue, typeof (T), dateFormats);
        }

        public static object Convert(this string stringValue, Type targetType, params string[] dateFormats)
        {
            if (targetType == typeof(string))
                return stringValue;

            var targetTypeInspector = targetType.Inspector();

            if (string.IsNullOrWhiteSpace(stringValue))
                return targetTypeInspector.DefaultValue();

            object returnValue = null;
            
            targetType = targetTypeInspector.RealType;
            
            if (_stringConverters != null)
                if (_stringConverters.Any(converter => converter.TryConvert(stringValue, targetType, out returnValue)))
                    return returnValue;

            var implicitOperator = targetTypeInspector.GetMethod("op_Implicit", new[] { typeof(string) });

            if (implicitOperator != null)
                return implicitOperator.Invoke(null, new[] { stringValue });

            if (targetTypeInspector.IsEnum)
            {
                if (char.IsNumber(stringValue, 0))
                {
                    long longValue;

                    if (Int64.TryParse(stringValue, out longValue))
                    {
                        returnValue = Enum.ToObject(targetType, longValue);

                        if (Enum.IsDefined(targetType, returnValue))
                            return returnValue;
                    }
                }
                else
                {
                    if (Enum.IsDefined(targetType, stringValue))
                        return Enum.Parse(targetType, stringValue, true);
                }

                return targetTypeInspector.DefaultValue();
            }

            if (targetTypeInspector.Is(TypeFlags.Single|TypeFlags.Double))
            {
                double doubleValue;

                if (!Double.TryParse(stringValue.Replace(',', '.'), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out doubleValue))
                    returnValue = null;
                else
                    returnValue = doubleValue;
            }
            else if (targetTypeInspector.Is(TypeFlags.Decimal))
            {
                decimal decimalValue;

                if (!Decimal.TryParse(stringValue.Replace(',', '.'), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out decimalValue))
                    returnValue = null;
                else
                    returnValue = decimalValue;
            }
            else if (targetTypeInspector.Is(TypeFlags.SignedInteger))
            {
                long longValue;

                if (!Int64.TryParse(stringValue, out longValue))
                    returnValue = null;
                else
                    returnValue = longValue;
            }
            else if (targetTypeInspector.Is(TypeFlags.UnsignedInteger))
            {
                ulong longValue;

                if (!UInt64.TryParse(stringValue, out longValue))
                    returnValue = null;
                else
                    returnValue = longValue;
            }
            else if (targetType == typeof (DateTime))
            {
                DateTime dateTime;

                if (dateFormats.Length == 0)
                    dateFormats = _dateFormats;

                if (!DateTime.TryParseExact(stringValue, dateFormats ?? _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out dateTime))
                {
                    if (!DateTime.TryParse(stringValue, out dateTime))
                    {
                        double? seconds = Convert<double?>(stringValue);

                        if (seconds == null)
                            returnValue = null;
                        else
                            returnValue = new DateTime(1970, 1, 1).AddSeconds(seconds.Value);
                    }
                }
                else
                    returnValue = dateTime;
            }
            else if (targetType == typeof (bool))
            {
                returnValue = (stringValue == "1" || stringValue.ToUpper() == "Y" || stringValue.ToUpper() == "YES" || stringValue.ToUpper() == "T" || stringValue.ToUpper() == "TRUE");
            }
            else if (targetType == typeof(char))
            {
                if (stringValue.Length == 1)
                    returnValue = stringValue[0];
                else
                    returnValue = null;
            }

            if (returnValue == null)
                return targetTypeInspector.DefaultValue();

            try
            {
                return System.Convert.ChangeType(returnValue, targetType, null);
            }
            catch
            {
                return targetTypeInspector.DefaultValue();
            }
        }
    }
}