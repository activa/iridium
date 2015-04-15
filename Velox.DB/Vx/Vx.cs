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

namespace Velox.DB
{
    public static partial class Vx
    {

        private static Context _defaultContext;

        public static IDataSet<T> DataSet<T>()
        {
            return Repository.CreateDataSet<T>();
        }

        public static Context DB
        {
            get { return _defaultContext; }
            set { _defaultContext = value; }
        }


        // TODO: wrap in proper statistics class
        public static int GetQueryCount<T>(IDataSet<T> dataSet)
        {
            return ((DataSet<T>) dataSet).Repository.QueryCount;
        }

        public static void ResetStats<T>(IDataSet<T> dataSet)
        {
            ((DataSet<T>) dataSet).Repository.QueryCount = 0;
        }


        // TODO: move this somewhere else

        public static List<T> CreateEmptyList<T>(T templateValue) { return new List<T>(); }
        public static Dictionary<TKey, TValue> CreateEmptyDictionary<TKey, TValue>(TKey keyTemplate, TValue valueTemplae) { return new Dictionary<TKey, TValue>(); }


    }
}