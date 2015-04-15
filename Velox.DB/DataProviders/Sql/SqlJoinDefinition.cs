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
            return sqlDialect.JoinSql(this);
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