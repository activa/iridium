using System;

namespace Velox.DB.Sql
{
    public class SqlJoinPart : IEquatable<SqlJoinPart>
    {
        public readonly OrmSchema Schema;
        public readonly OrmSchema.Field Field;
        public readonly string Alias;

        public SqlJoinPart(OrmSchema schema, OrmSchema.Field field, string alias)
        {
            Field = field;
            Schema = schema;
            Alias = alias;
        }

        public bool Equals(SqlJoinPart other)
        {
            return /*Alias == other.Alias && */Schema == other.Schema && Field == other.Field;
        }

        public override int GetHashCode()
        {
            return Schema.GetHashCode() ^ Field.GetHashCode();// ^ Alias.GetHashCode();
        }
    }
}