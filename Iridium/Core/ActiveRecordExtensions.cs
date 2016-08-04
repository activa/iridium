#region License
//=============================================================================
// Iridium - Porable .NET ORM 
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
using System.Linq.Expressions;

namespace Iridium.DB
{
    public static class IridiumExtensions
    {
        public static bool InsertOrUpdate<T>(this T entity, bool saveRelations = false, bool? create = null) where T:IEntity
        {
            return Ir.DataSet<T>().InsertOrUpdate(entity, saveRelations);
        }

        public static bool Save<T>(this T entity, bool saveRelations = false, bool? create = null) where T : IEntity
        {
            return Ir.DataSet<T>().InsertOrUpdate(entity, saveRelations);
        }

        public static bool Insert<T>(this T entity, bool saveRelations = false) where T : IEntity
        {
            return Ir.DataSet<T>().Insert(entity, saveRelations);
        }

        public static bool Update<T>(this T entity, bool saveRelations = false) where T : IEntity
        {
            return Ir.DataSet<T>().Update(entity, saveRelations);
        }

        public static T Load<T>(this T obj, object key, params Expression<Func<T, object>>[] relationsToLoad) where T : IEntity
        {
            return Ir.DataSet<T>().Load(obj, key, relationsToLoad);
        }

        public static bool Delete<T>(this T entity) where T : IEntity
        {
            return Ir.DataSet<T>().Delete(entity);
        }

        public static T WithRelations<T>(this T entity, params Expression<Func<T, object>>[] relations) where T : IEntity
        {
            DbContext.Instance.LoadRelations(entity, relations);

            return entity;
        }


    }
}
