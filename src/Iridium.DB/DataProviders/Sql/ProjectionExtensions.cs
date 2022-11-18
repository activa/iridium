using System;
using System.Collections.Generic;
using System.Linq;

namespace Iridium.DB
{
    internal static class ProjectionExtensions
    {
        internal static IEnumerable<TableSchema.Field> Fields(this ProjectionInfo projectionInfo, TableSchema Schema)
        {
            var fields = new HashSet<TableSchema.Field>(Schema.Fields);

            if (projectionInfo == null)
                return fields;

            if (projectionInfo.ProjectedFields != null)
                fields = new HashSet<TableSchema.Field>(projectionInfo.ProjectedFields);

            if (projectionInfo.FullSchemasReferenced != null)
            {
                foreach (var referencedSchema in projectionInfo.FullSchemasReferenced)
                {
                    fields.UnionWith(referencedSchema.Fields);
                }
            }

            return fields;
        }
    }
}