using System.Collections.Generic;

namespace Velox.DB
{
    public abstract class QueryExpression
    {
        public abstract object Evaluate(object target);
        public abstract HashSet<OrmSchema.Relation> FindRelations(OrmSchema schema);
    }
}