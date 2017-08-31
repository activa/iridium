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

namespace Iridium.DB
{
    public class SqlJoinPart : IEquatable<SqlJoinPart>
    {
        public TableSchema Schema { get; }
        public TableSchema.Field Field { get; }
        public string Alias { get; }

        public SqlJoinPart(TableSchema schema, TableSchema.Field field, string alias)
        {
            Field = field;
            Schema = schema;
            Alias = alias;
        }

        public bool Equals(SqlJoinPart other)
        {
            return Schema == other.Schema && Field == other.Field;
        }

        public override int GetHashCode()
        {
            return Schema.GetHashCode() ^ Field.GetHashCode();
        }
    }
}