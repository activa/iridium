using System;

namespace Velox.DB.Sql
{
    public enum SqlJoinType
    {
        Inner,LeftOuter
    }

    public class SqlJoinDefinition : IEquatable<SqlJoinDefinition>
    {
        public SqlJoinPart Left;
        public SqlJoinPart Right;
        public SqlJoinType Type;

        public bool Equals(SqlJoinDefinition other)
        {
            return Left.Equals(other.Left) && Right.Equals(other.Right);
        }

        public string ToSql(SqlDialect sqlDialect)
        {
            return sqlDialect.InnerJoinSql(this);
        }

        public override bool Equals(object obj)
        {
            return Equals((SqlJoinDefinition)obj);
        }

        public override int GetHashCode()
        {
            return Left.GetHashCode() ^ Right.GetHashCode();
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("inner join {0} {1} on {2}={3}",
                Right.Schema.MappedName,
                Right.Alias,
                Left.Alias + "." + Left.Field.MappedName,
                Right.Alias + "." + Right.Field.MappedName);
        }
#endif
    }
}