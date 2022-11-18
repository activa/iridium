using System;
using System.Collections.Generic;
using System.Linq;

namespace Iridium.DB
{
    public class ProjectionInfo
    {
        public ProjectionInfo(HashSet<TableSchema.Field> projectedFields, HashSet<TableSchema.Relation> relationsReferenced, IEnumerable<TableSchema> fullSchemasReferenced)
        {
            ProjectedFields = projectedFields;
            RelationsReferenced = relationsReferenced;
            FullSchemasReferenced = new HashSet<TableSchema>(fullSchemasReferenced);
        }

        public HashSet<TableSchema.Field> ProjectedFields { get; }
        public HashSet<TableSchema.Relation> RelationsReferenced { get; }
        public HashSet<TableSchema> FullSchemasReferenced { get; }
        public bool Distinct { get; set; }
    }
}