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
using System.Collections.Generic;
using System.Linq;
using Velox.DB.Core;

namespace Velox.DB
{
    public partial class OrmSchema
    {

        private readonly string _mappedName;
        private readonly Type _objectType;
        private readonly SafeDictionary<string, Field> _fields = new SafeDictionary<string, Field>();
        private readonly SafeDictionary<string, Field> _mappedFields = new SafeDictionary<string, Field>();
        private readonly Field[] _fieldList;
        
        private SafeDictionary<string, Relation> _relations;
        private readonly Field[] _primaryKeys;
        private readonly Field[] _incrementKeys;
        private readonly Index[] _indexes;

        private readonly Repository _repository;

        private static readonly List<Func<TypeInspector,bool>> _mappableTypes;

        static OrmSchema()
        {
            _mappableTypes = new List<Func<TypeInspector, bool>>()
            {
                t => t.Is(TypeFlags.Array | TypeFlags.Byte),
                t => t.Is(TypeFlags.Numeric | TypeFlags.String | TypeFlags.DateTime | TypeFlags.Boolean) && !t.Is(TypeFlags.Array)
            };
        }

        internal OrmSchema(Type t, Repository repository)
        {
            _objectType = t;
            _repository = repository;

            _mappedName = t.Name;

            var tableNameAttribute = t.Inspector().GetAttribute<Table.NameAttribute>(false);

            if (tableNameAttribute != null)
                _mappedName = tableNameAttribute.Name;

            var indexedFields = Vx.CreateEmptyList(new { IndexName = "", Position = 0, SortOrder = SortOrder.Ascending, Field = (Field)null });

            var fieldList = new List<Field>();

            foreach (var field in t.Inspector().GetFieldsAndProperties(BindingFlags.Instance|BindingFlags.Public).Where(field => _mappableTypes.Any(f => f(field.FieldType.Inspector()))))
            {
                var fieldInspector = field.Inspector();

                if (fieldInspector.HasAttribute<Column.IgnoreAttribute>())
                    continue;

                var schemaField = new Field(field);

                var fieldPropertiesFromConvention = Vx.Config.NamingConvention.GetFieldProperties(this, schemaField);

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
                else if (field.FieldType == typeof (string))
                {
                    if (fieldInspector.HasAttribute<Column.LargeTextAttribute>())
                        schemaField.ColumnSize = int.MaxValue;
                    else
                        schemaField.ColumnSize = 50;
                }
                else if (field.FieldType.Inspector().Is(TypeFlags.Decimal))
                {
                    schemaField.ColumnSize = 10;
                    schemaField.ColumnScale = 5;
                }

                if (fieldInspector.HasAttribute<Column.PrimaryKeyAttribute>())
                {
                    var pkAttribute = fieldInspector.GetAttribute<Column.PrimaryKeyAttribute>();

                    schemaField.PrimaryKey = true;
                    schemaField.AutoIncrement = pkAttribute.AutoIncrement;
                }
                else if (fieldPropertiesFromConvention.PrimaryKey ?? false)
                {
                    schemaField.PrimaryKey = true;

                    if (fieldPropertiesFromConvention.AutoIncrement != null)
                        schemaField.AutoIncrement = fieldPropertiesFromConvention.AutoIncrement.Value;
                }

                if (fieldPropertiesFromConvention.Null != null)
                    schemaField.ColumnNullable = fieldPropertiesFromConvention.Null.Value;

                if (fieldInspector.HasAttribute<Column.NotNullAttribute>())
                    schemaField.ColumnNullable = false;

                if (fieldInspector.HasAttribute<Column.NullAttribute>())
                    schemaField.ColumnNullable = true;

                if (fieldInspector.HasAttribute<Column.IndexedAttribute>() || (fieldPropertiesFromConvention.Indexed ?? false))
                {
                    var indexAttribute = fieldInspector.GetAttribute<Column.IndexedAttribute>();

                    if (indexAttribute != null)
                    {
                        indexedFields.Add(new
                        {
                            IndexName = indexAttribute.IndexName ?? MappedName + schemaField.MappedName,
                            Position = indexAttribute.Position,
                            SortOrder = indexAttribute.Descending ? SortOrder.Descending : SortOrder.Ascending,
                            Field = schemaField
                        });
                    }
                    else
                    {
                        indexedFields.Add(new
                        {
                            IndexName = MappedName + schemaField.MappedName,
                            Position = 0,
                            SortOrder = SortOrder.Ascending,
                            Field = schemaField
                        });
                    }
                }

                _fields[schemaField.FieldName] = schemaField;
                _mappedFields[schemaField.MappedName] = schemaField;

                fieldList.Add(schemaField);
            }

            _indexes = indexedFields
                        .ToLookup(indexField => indexField.IndexName)
                        .Select(item => new Index
                                            {
                                                Name = item.Key,
                                                FieldsWithOrder = item.OrderBy(f => f.Position).Select(f => new Tuple<Field, SortOrder>(f.Field, f.SortOrder)).ToArray()
                                            })
                        .ToArray();

            _fieldList = fieldList.ToArray();

            _primaryKeys = _fieldList.Where(f => f.PrimaryKey).ToArray();
            _incrementKeys = _fieldList.Where(f => f.AutoIncrement).ToArray();
        }

        private SafeDictionary<string,Relation> FindRelations()
        {
            var relations = new SafeDictionary<string, Relation>();

            foreach (var field in ObjectType.Inspector().GetFieldsAndProperties(BindingFlags.Instance | BindingFlags.Public).Where(field => !_mappableTypes.Any(f => f(field.FieldType.Inspector()))))
            {
                Type collectionType = field.FieldType.Inspector().GetInterfaces().FirstOrDefault(tI => tI.IsConstructedGenericType && tI.GetGenericTypeDefinition() == typeof (IEnumerable<>));
                
                if (field.Inspector().HasAttribute<DB.Relation.IgnoreAttribute>())
                    continue;

                Relation relation = new Relation(field)
                {
                    LocalSchema = this
                };

                if (collectionType != null)
                {
                    if (PrimaryKeys.Length != 1)
                        continue;

                    bool isDataSet = field.FieldType.IsConstructedGenericType && field.FieldType.GetGenericTypeDefinition() == typeof (IDataSet<>);

                    Type elementType = collectionType.GenericTypeArguments[0];

                    var foreignSchema = Repository.Context.GetSchema(elementType);

                    if (foreignSchema == null)
                        continue;

                    relation.RelationType = RelationType.OneToMany;
                    relation.ForeignSchema = foreignSchema;
                    relation.ElementType = elementType;
                    relation.IsDataSet = isDataSet;
                    relation.LocalField = PrimaryKeys[0];
                    relation.ForeignField = Vx.Config.NamingConvention.GetRelationField(relation);
                }
                else
                {
                    Type objectType = field.FieldType;

                    var foreignSchema = Repository.Context.GetSchema(objectType);

                    var relationAttribute = field.Inspector().GetAttribute<DB.Relation.ManyToOneAttribute>();

                    if (foreignSchema == null)
                        continue;

                    relation.RelationType = RelationType.ManyToOne;
                    relation.ReadOnly = relationAttribute != null && relationAttribute.ReadOnly;
                    relation.ForeignSchema = foreignSchema;

                    var localField = Vx.Config.NamingConvention.GetRelationField(relation);
                    var foreignField = foreignSchema.PrimaryKeys.Length == 1 ? foreignSchema.PrimaryKeys[0] : null;

                    if (relationAttribute != null)
                    {
                        if (relationAttribute.ForeignKey != null)
                            foreignField = foreignSchema.Fields[relationAttribute.ForeignKey];

                        if (relationAttribute.LocalKey != null)
                            localField = Fields[relationAttribute.LocalKey];
                    }

                    relation.LocalField = localField;
                    relation.ForeignField = foreignField;
                }

                if (relation.ForeignField != null && relation.LocalField != null)
                {
                    relations[field.Name] = relation;
                }


            }

            var dataSetRelations = new HashSet<Relation>(relations.Values.Where(r => r.IsDataSet));

            DatasetRelations = dataSetRelations.Any() ? new HashSet<Relation>(dataSetRelations) : null;

            return relations;
        }

        public SafeDictionary<string,Field> Fields
        {
            get { return _fields; }
        }

        public Field[] FieldList
        {
            get { return _fieldList; }
        }

        public SafeDictionary<string, Relation> Relations
        {
            get { return _relations ?? (_relations = FindRelations()); }
        }

        internal void InvalidateRelations()
        {
            _relations = null;
        }

        public Field[] PrimaryKeys
        {
            get { return _primaryKeys; }
        }

        public Field[] IncrementKeys
        {
            get { return _incrementKeys; }
        }

        internal Repository Repository
        {
            get { return _repository; }
        }

        public Type ObjectType
        {
            get { return _objectType; }
        }

        public string MappedName
        {
            get { return _mappedName; }
        }

        internal HashSet<Relation> DatasetRelations { get; private set; }

        public Index[] Indexes
        {
            get { return _indexes; }
        }

        internal object UpdateObject(object o, SerializedEntity entity)
        {
            foreach (var fieldName in entity.FieldNames)
            {
                _mappedFields[fieldName].SetField(o, entity[fieldName]);
            }

            return o;
        }

        internal T UpdateObject<T>(T o, SerializedEntity entity)
        {
            foreach (var fieldName in entity.FieldNames)
            {
                _mappedFields[fieldName].SetField(o, entity[fieldName]);
            }

            return o;
        }

        internal SerializedEntity SerializeObject(object o)
        {
            return new SerializedEntity((from field in _mappedFields select new { field.Key, Value = field.Value.GetField(o)}).ToDictionary(k => k.Key, k=> k.Value) );
        }

#if DEBUG
        public override string ToString()
        {
            return "<" + ObjectType.Name + ">";
        }
#endif
    }
}