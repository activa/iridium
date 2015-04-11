using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Velox.DB
{
    public static class VxExtensions
    {
        public static bool Save<T>(this T entity, bool saveRelations = false, bool? create = null) where T:IEntity
        {
            return Vx.DataSet<T>().Save(entity, saveRelations, create);
        }

        public static bool Create<T>(this T entity, bool saveRelations = false) where T : IEntity
        {
            return Vx.DataSet<T>().Create(entity, saveRelations);
        }

        public static T Load<T>(this T obj, object key, params Expression<Func<T, object>>[] relationsToLoad) where T : IEntity
        {
            return Vx.DataSet<T>().Load(obj, key, relationsToLoad);
        }

        public static bool Delete<T>(this T entity) where T : IEntity
        {
            return Vx.DataSet<T>().Delete(entity);
        }

        public static T WithRelations<T>(this T entity, params Expression<Func<T, object>>[] relations) where T : IEntity
        {
            Vx.LoadRelations(entity, relations);

            return entity;
        }


    }
}
