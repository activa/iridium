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
using System.Globalization;

#if IRIDIUM_CORE_EMBEDDED
namespace Iridium.DB.CoreUtil
#else
namespace Iridium.Core
#endif
{
    public static class ObjectConverter
    {
        public static T Convert<T>(this object value)
        {
            return (T) Convert(value, typeof (T));
        }

        public static object Convert(this object value, Type targetType)
        {
            if (targetType == typeof(object))
                return value;

            if (value is string)
                return StringConverter.Convert((string)value, targetType);

            var targetTypeInspector = targetType.Inspector();

            if (value == null)
				return targetTypeInspector.DefaultValue();

			targetType = targetTypeInspector.RealType;

			Type sourceType = value.GetType();

			if (sourceType == targetType)
                return value;

            var sourceTypeInspector = sourceType.Inspector();

			var implicitOperator = targetTypeInspector.GetMethod("op_Implicit", new [] {sourceType});

            if (implicitOperator != null)
                return implicitOperator.Invoke(null, new [] {value});


			if (targetType == typeof(string))
            {
                if (sourceTypeInspector.Is(TypeFlags.Decimal))
                    return ((decimal)value).ToString(CultureInfo.InvariantCulture);
                if (sourceTypeInspector.Is(TypeFlags.FloatingPoint))
                    return System.Convert.ToDouble(value).ToString(CultureInfo.InvariantCulture);

                return value.ToString();
            }

			if (targetType == typeof (Guid) && value is byte[])
                return new Guid((byte[]) value);

			if (targetType == typeof (byte[]) && value is Guid)
                return ((Guid) value).ToByteArray();

			if (targetTypeInspector.IsEnum)
            {
                try
                {
                    value = System.Convert.ToInt64(value);

					value = Enum.ToObject(targetType, value);
                }
                catch
                {
                    return targetTypeInspector.DefaultValue();
                }

                if (Enum.IsDefined(targetType, value))
                    return value;

                if (!char.IsDigit(value.ToString()[0]))
                    return value;

                return targetTypeInspector.DefaultValue();
            }

			if (targetTypeInspector.IsAssignableFrom(value.GetType()))
                return value;

			if (targetType.IsArray && sourceType.IsArray)
			{
				Type targetArrayType = targetType.GetElementType();
				Array sourceArray = (Array) value;

				Array array = Array.CreateInstance(targetArrayType, new [] { sourceArray.Length }, new [] { 0 });

				for (int i = 0; i < sourceArray.Length; i++)
				{
					array.SetValue(sourceArray.GetValue(i).Convert(targetArrayType), i);
				}

				return array;
			}

            if (targetTypeInspector.Is(TypeFlags.DateTime))
                return ToDateTime(value, sourceTypeInspector) ?? targetTypeInspector.DefaultValue();

            if (targetType == typeof(TimeSpan))
                return ToTimeSpan(value, sourceTypeInspector) ?? targetTypeInspector.DefaultValue();

            try
            {
				return System.Convert.ChangeType(value, targetType, null);
            }
            catch
            {
                return targetTypeInspector.DefaultValue();
            }
        }

        private static DateTime? ToDateTime(object value, TypeInspector type)
        {
            if (type.Is(TypeFlags.Integer64)) // Assume .NET ticks
            {
                return new DateTime(System.Convert.ToInt64(value));
            }

            if (type.Is(TypeFlags.Integer32)) // Assume Unix seconds since 1970-01-01
            {
                return new DateTime(1970,1,1).AddSeconds(System.Convert.ToInt32(value));
            }

            if (type.Is(TypeFlags.FloatingPoint))
            {
                return JulianToDateTime(System.Convert.ToDouble(value));
            }

            return null;
        }

        private static DateTime JulianToDateTime(double julian)
        {
            long L = (long)julian + 68569;
            long N = (4 * L) / 146097;
            L = L - (146097 * N + 3) / 4;
            long I = 4000 * (L + 1) / 1461001;
            L = L - (1461 * I) / 4 + 31;
            long J = (80 * L) / 2447;
            int day = (int)(L - (long)((2447 * J) / 80));
            L = J / 11;
            int month = (int)(J + 2 - 12 * L);
            int year = (int)(100 * (N - 49) + I + L);

            return new DateTime(year,month,day).Add(TimeSpan.FromDays(julian-(long)julian));
        }

        private static TimeSpan? ToTimeSpan(object value, TypeInspector type)
        {
            if (type.Is(TypeFlags.Integer64))
            {
                return new TimeSpan(System.Convert.ToInt64(value));
            }

            if (type.Is(TypeFlags.Numeric))
            {
                return TimeSpan.FromSeconds(System.Convert.ToDouble(value));
            }

            return null;
        }

    }
}