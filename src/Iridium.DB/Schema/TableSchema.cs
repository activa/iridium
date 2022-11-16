﻿#region License
//=============================================================================
// Iridium - Porable .NET ORM 
//
// Copyright (c) 2015-2017 Philippe Leybaert
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
using System.Collections.Generic;
using System.Linq;
using Iridium.Reflection;

namespace Iridium.DB
{
    public partial class TableSchema
    {
        internal Repository Repository { get; }

        public SafeDictionary<string, Field> FieldsByMappedName { get; } = new SafeDictionary<string, Field>();
        public SafeDictionary<string, Field> FieldsByFieldName { get; } = new SafeDictionary<string, Field>();
        public Field[] Fields { get; private set; }
        public Field[] WriteFields { get; private set; }
        public SafeDictionary<string, Relation> Relations { get; private set; }
        public Field[] PrimaryKeys { get; private set; }
        public Field IncrementKey { get; private set; }
        public Type ObjectType { get; }
        public string MappedName { get; }
        public HashSet<Relation> DatasetRelations { get; private set; }
        public HashSet<Relation> PreloadRelations { get; private set; }
        public Index[] Indexes { get; private set; }

        private static readonly List<Func<TypeInspector, bool>> _mappableTypes = new List<Func<TypeInspector, bool>>
            {
                t => t.Is(TypeFlags.Array | TypeFlags.Byte),
                t => t.Is(TypeFlags.Numeric | TypeFlags.String | TypeFlags.DateTime | TypeFlags.Boolean | TypeFlags.Guid) && !t.Is(TypeFlags.Array)
            };

        internal TableSchema(Type t, Repository repository)
        {
            ObjectType = t;
            Repository = repository;

            MappedName = t.Name;

            var tableNameAttribute = t.Inspector().GetAttribute<Table.NameAttribute>(false);

            if (tableNameAttribute != null)
                MappedName = tableNameAttribute.Name;

            FindFields();
        }

        private void FindFields()
        {
            var indexedFields = Ir.CreateEmptyList(new {IndexName = "", Position = 0, SortOrder = SortOrder.Ascending, Field = (Field) null, Unique = false});

            var fieldList = new List<Field>();

            var mappableFields = ObjectType.Inspector().GetFieldsAndProperties(BindingFlags.Instance | BindingFlags.Public).Where(field => _mappableTypes.Any(f => f(field.Type.Inspector()))).ToArray();

            bool hasExplicitPrimaryKey = mappableFields.Any(f => f.HasAttribute<Column.PrimaryKeyAttribute>());
            bool hasExplicitIndexes = mappableFields.Any(f => f.HasAttribute<Column.IndexedAttribute>());

            foreach (var field in mappableFields)
            {
                var fieldInspector = field;

                if (fieldInspector.HasAttribute<Column.IgnoreAttribute>() || (fieldInspector.IsProperty && !fieldInspector.IsWritePublic))
                    continue;

                var schemaField = new Field(field.MemberInfo);

                var fieldPropertiesFromConvention = Ir.Config.NamingConvention.GetFieldProperties(this, schemaField);

                if (fieldPropertiesFromConvention.MappedTo != null)
                    schemaField.MappedName = fieldPropertiesFromConvention.MappedTo;

                if (fieldInspector.HasAttribute<Column.NameAttribute>())
                {
                    schemaField.MappedName = fieldInspector.GetAttribute<Column.NameAttribute>().Name;
                }

                if (fieldInspector.HasAttribute<Column.SizeAttribute>())
                {
                    schemaField.ColumnSize = fieldInspector.GetAttribute<Column.SizeAttribute>().Size;
                    schemaField.ColumnScale = fieldInspector.GetAttribute<Column.SizeAttribute>().Scale;
                }
                else if (field.Type == typeof (string))
                {
                    schemaField.ColumnSize = fieldInspector.HasAttribute<Column.LargeTextAttribute>() ? int.MaxValue : 50;
                }
                else if (field.Type.Inspector().Is(TypeFlags.Decimal))
                {
                    schemaField.ColumnSize = 10;
                    schemaField.ColumnScale = 5;
                }

                if (fieldInspector.HasAttribute<Column.PrimaryKeyAttribute>())
                {
                    var pkAttribute = fieldInspector.GetAttribute<Column.PrimaryKeyAttribute>();

                    schemaField.UpdateFlags(FieldFlags.PrimaryKey, true);
                    schemaField.UpdateFlags(FieldFlags.AutoIncrement, pkAttribute.AutoIncrement);
                }
                else if (!hasExplicitPrimaryKey && (fieldPropertiesFromConvention.PrimaryKey ?? false) && !fieldInspector.HasAttribute<Column.NoPrimaryKeyAttribute>())
                {
                    schemaField.UpdateFlags(FieldFlags.PrimaryKey, true);
                    schemaField.UpdateFlags(FieldFlags.AutoIncrement, fieldPropertiesFromConvention.AutoIncrement);
                }

                schemaField.UpdateFlags(FieldFlags.Nullable, fieldPropertiesFromConvention.Null);

                if (fieldInspector.HasAttribute<Column.NotNullAttribute>())
                    schemaField.UpdateFlags(FieldFlags.Nullable, false);

                if (fieldInspector.HasAttribute<Column.NullAttribute>())
                    schemaField.UpdateFlags(FieldFlags.Nullable, true);

                if (fieldInspector.HasAttribute<Column.ReadOnlyAttribute>())
                    schemaField.UpdateFlags(FieldFlags.ReadOnly, true);

                if (schemaField.PrimaryKey)
                    schemaField.UpdateFlags(FieldFlags.Nullable, false);

                if (fieldInspector.HasAttribute<Column.IndexedAttribute>() || ((fieldPropertiesFromConvention.Indexed ?? false) && !fieldInspector.HasAttribute<Column.NotIndexedAttribute>()))
                {
                    var indexAttributes = fieldInspector.GetAttributes<Column.IndexedAttribute>();

                    if (indexAttributes.Length == 0 && !hasExplicitIndexes)
                        indexAttributes = new[] {new Column.IndexedAttribute()};

                    foreach (var indexAttribute in indexAttributes)
                    {
                        indexedFields.Add(new
                        {
                            IndexName = indexAttribute.IndexName ?? MappedName + schemaField.MappedName,
                            Position = indexAttribute.Position,
                            SortOrder = indexAttribute.Descending ? SortOrder.Descending : SortOrder.Ascending,
                            Field = schemaField,
                            Unique = indexAttribute.Unique
                        });
                    }
                }

                FieldsByFieldName[schemaField.FieldName] = schemaField;
                FieldsByMappedName[schemaField.MappedName] = schemaField;

                fieldList.Add(schemaField);
            }

            Indexes = indexedFields
                .ToLookup(indexField => indexField.IndexName)
                .Select(item => new Index
                {
                    Name = item.Key,
                    FieldsWithOrder = item.OrderBy(f => f.Position).Select(f => new Tuple<Field, SortOrder>(f.Field, f.SortOrder)).ToArray(),
                    Unique = item.All(arg => arg.Unique)
                })
                .ToArray();

            Fields = fieldList.ToArray();
            WriteFields = fieldList.Where(f => !f.ColumnReadOnly && !f.AutoIncrement).ToArray();

            PrimaryKeys = Fields.Where(f => f.PrimaryKey).ToArray();
            IncrementKey = Fields.FirstOrDefault(f => f.AutoIncrement);
        }

        internal void UpdateReverseRelations()
        {
            foreach (var relation in Relations.Values)
            {
                if (relation.RelationType == RelationType.OneToMany)
                {
                    relation.ReverseRelation = relation
                        .ForeignSchema
                        .Relations.Values
                        .FirstOrDefault(r => r.RelationType == RelationType.ManyToOne && r.ForeignSchema == this && r.ForeignField == relation.LocalField);
                }
                else if (relation.RelationType == RelationType.OneToOne)
                {
                    relation.ReverseRelation = relation
                        .ForeignSchema
                        .Relations.Values
                        .FirstOrDefault(r => r.RelationType == RelationType.OneToOne && r.ForeignSchema == this);
                }
            }
        }

        internal void UpdateRelations()
        {
            var relations = new SafeDictionary<string, Relation>();

            foreach (var field in ObjectType.Inspector().GetFieldsAndProperties(BindingFlags.Instance | BindingFlags.Public).Where(field => !_mappableTypes.Any(f => f(field.Type.Inspector()))))
            {
                Type collectionType = field.Type.Inspector().GetInterfaces().FirstOrDefault(tI => tI.IsConstructedGenericType && tI.GetGenericTypeDefinition() == typeof (IEnumerable<>));
                bool isDataSet = field.Type.IsConstructedGenericType && field.Type.GetGenericTypeDefinition() == typeof(IDataSet<>);

                var relationAttribute = field.GetAttribute<RelationAttribute>();
                var ignoreAttribute = field.GetAttribute<Iridium.DB.Relation.IgnoreAttribute>();
                var preloadAttribute = field.GetAttribute<Iridium.DB.Relation.PreloadAttribute>();

                if (ignoreAttribute != null)
                    continue;

                if (relationAttribute == null && !isDataSet)
                    continue;

                Field foreignField;
                Field localField;
                TableSchema foreignSchema;

                Relation relation = new Relation(field.MemberInfo)
                {
                    LocalSchema = this,
                    Preload = preloadAttribute != null
                };

                if (collectionType != null)
                {
                    if (PrimaryKeys.Length != 1)
                        continue;

                    Type elementType = collectionType.GenericTypeArguments[0];

                    foreignSchema = Repository.Context.GetSchema(elementType) ?? throw new SchemaException($"Could not create relation {ObjectType.Name}.{field.Name}");

                    relation.RelationType = RelationType.OneToMany;
                    relation.ElementType = elementType;
                    relation.IsDataSet = isDataSet;
                    relation.ForeignSchema = foreignSchema;

                    localField = PrimaryKeys[0];
                    foreignField = Ir.Config.NamingConvention.GetRelationField(relation);
                }
                else
                {
                    Type objectType = field.Type;

                    if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Lazy<>))
                        objectType = objectType.GenericTypeArguments[0];

                    foreignSchema = Repository.Context.GetSchema(objectType) ?? throw new SchemaException($"Could not create relation {ObjectType.Name}.{field.Name}");

                    relation.RelationType = relationAttribute is DB.Relation.OneToOneAttribute ? RelationType.OneToOne : RelationType.ManyToOne;
                    relation.ReadOnly = relationAttribute != null && relationAttribute.ReadOnly;
                    relation.ForeignSchema = foreignSchema;

                    localField = Ir.Config.NamingConvention.GetRelationField(relation);
                    foreignField = foreignSchema.PrimaryKeys.Length == 1 ? foreignSchema.PrimaryKeys[0] : null;
                }

                if (relationAttribute != null)
                {
                    if (relationAttribute.ForeignKey != null)
                        foreignField = foreignSchema.FieldsByFieldName[relationAttribute.ForeignKey];

                    if (relationAttribute.LocalKey != null)
                        localField = FieldsByFieldName[relationAttribute.LocalKey];
                }

                if (localField == null || foreignField == null)
                    throw new SchemaException($"Could not create relation {ObjectType.Name}.{field.Name}");

                relation.LocalField = localField;
                relation.ForeignField = foreignField;
                

                relations[field.Name] = relation;
            }

            var dataSetRelations = new HashSet<Relation>(relations.Values.Where(r => r.IsDataSet));
            var preloadRelations = new HashSet<Relation>(relations.Values.Where(r => r.Preload));

            DatasetRelations = dataSetRelations.Any() ? dataSetRelations : null;
            PreloadRelations = preloadRelations.Any() ? preloadRelations : null;

            Relations = relations;
        }

        internal object UpdateObject(object o, SerializedEntity entity)
        {
            foreach (var fieldName in entity.FieldNames)
            {
                FieldsByMappedName[fieldName].SetField(o, entity[fieldName]);
            }

            return o;
        }

        internal T UpdateObject<T>(T o, SerializedEntity entity)
        {
            foreach (var fieldName in entity.FieldNames)
            {
                FieldsByMappedName[fieldName].SetField(o, entity[fieldName]);
            }

            return o;
        }

        internal SerializedEntity SerializeObject(object o)
        {
            return new SerializedEntity(
                (
                    from field in FieldsByMappedName
                    select new { field.Key, Value = field.Value.GetField(o)}
                )
                .ToDictionary(k => k.Key, k=> k.Value) 
            );
        }

        internal HashSet<Relation> BuildPreloadRelationSet(HashSet<Relation> relations = null)
        {
            if (DatasetRelations != null)
            {
                if (relations == null)
                    relations = new HashSet<TableSchema.Relation>(DatasetRelations);
                else
                    relations.UnionWith(DatasetRelations);
            }

            if (PreloadRelations != null)
            {
                if (relations == null)
                    relations = new HashSet<TableSchema.Relation>(PreloadRelations);
                else
                    relations.UnionWith(PreloadRelations);
            }

            return relations;
        }

#if DEBUG
        public override string ToString()
        {
            return $"<{ObjectType.Name}>";
        }
#endif
    }

}