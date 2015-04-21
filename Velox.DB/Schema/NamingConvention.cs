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
using System.Text.RegularExpressions;
using Velox.DB.Core;

namespace Velox.DB
{
    public class NamingConvention
    {
        public string PrimaryKeyName { get; set; }
        public string ManyToOneLocalKeyName { get; set; }
        public string OneToManyForeignKeyName { get; set; }
        public string IsFieldIndexedRegex { get; set; }

        public bool UseAutoIncrement { get; set; }

        public const string CLASS_NAME = "{Class.Name}";
        public const string CLASS_PRIMARYKEY = "{Class.PrimaryKey}";
        public const string RELATION_CLASS_NAME = "{Relation.Class.Name}";
        public const string RELATION_CLASS_PRIMARYKEY = "{Relation.Class.PrimaryKey}";

        public NamingConvention()
        {
            PrimaryKeyName = CLASS_NAME + "Id";
            ManyToOneLocalKeyName = RELATION_CLASS_PRIMARYKEY;
            OneToManyForeignKeyName = CLASS_PRIMARYKEY;
            IsFieldIndexedRegex = "ID$";
            UseAutoIncrement = true;
        }

        public virtual FieldProperties GetFieldProperties(OrmSchema schema, OrmSchema.Field field)
        {
            var fieldProperties = new FieldProperties();

            if (Regex.IsMatch(field.FieldName, IsFieldIndexedRegex, RegexOptions.IgnoreCase))
                fieldProperties.Indexed = true;

            string pkName = PrimaryKeyName.Replace(CLASS_NAME, schema.ObjectType.Name);

            if (field.FieldName.Equals(pkName, StringComparison.OrdinalIgnoreCase))
            {
                fieldProperties.PrimaryKey = true;
                fieldProperties.AutoIncrement = UseAutoIncrement && field.FieldType.Inspector().Is(TypeFlags.Integer);
                fieldProperties.Indexed = false;
            }

            return fieldProperties;
        }

        public virtual OrmSchema.Field GetRelationField(OrmSchema.Relation relation)
        {
            if (relation.LocalSchema.PrimaryKeys.Length < 1 || relation.ForeignSchema.PrimaryKeys.Length < 1)
                return null;

            string relationKeyName = ((relation.RelationType == OrmSchema.RelationType.ManyToOne) ? ManyToOneLocalKeyName : OneToManyForeignKeyName)
                .Replace(RELATION_CLASS_PRIMARYKEY, relation.ForeignSchema.PrimaryKeys[0].FieldName)
                .Replace(RELATION_CLASS_NAME, relation.ForeignSchema.ObjectType.Name)
                .Replace(CLASS_PRIMARYKEY, relation.LocalSchema.PrimaryKeys[0].FieldName)
                .Replace(CLASS_NAME, relation.LocalSchema.ObjectType.Name);

            return relation.RelationType == OrmSchema.RelationType.ManyToOne ? relation.LocalSchema.Fields[relationKeyName] : relation.ForeignSchema.Fields[relationKeyName];
        }

        public class FieldProperties
        {
            public bool? PrimaryKey { get; set; }
            public bool? AutoIncrement { get; set; }
            public bool? Indexed { get; set; }
            public string MappedTo { get; set; }
            public bool? Null { get; set; }
        }
    }
}