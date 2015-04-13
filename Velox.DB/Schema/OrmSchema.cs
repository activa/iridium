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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
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
        private readonly Repository _repository;

        private static readonly HashSet<Type> _mappableTypes;

        static OrmSchema()
        {
            _mappableTypes = new HashSet<Type>(new[]
            {
                typeof(Boolean), typeof(Byte), typeof(SByte), typeof(UInt16), typeof(UInt32), typeof(UInt64), typeof(Int16), typeof(Int32), typeof(Int64), typeof(Single), typeof(Double), typeof(Decimal), typeof(string), typeof(DateTime), typeof(byte[])
            });
        }

        internal OrmSchema(Type t, Repository repository)
        {
            _objectType = t;
            _repository = repository;

            _mappedName = t.Name;

            var fieldList = new List<Field>();

            foreach (var field in t.Inspector().GetFieldsAndProperties(BindingFlags.Instance|BindingFlags.Public).Where(f => _mappableTypes.Contains(f.FieldType.Inspector().RealType)))
            {
                var fieldInspector = field.Inspector();

                var schemaField = new Field(field);

                var fieldPropertiesFromConvention = Vx.Config.NamingConvention.GetFieldProperties(this, schemaField);

                if (fieldPropertiesFromConvention.MappedTo != null)
                    schemaField.MappedName = fieldPropertiesFromConvention.MappedTo;

                if (fieldInspector.HasAttribute<MapToAttribute>())
                {
                    schemaField.MappedName = fieldInspector.GetAttribute<MapToAttribute>().Name;
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

                if (fieldInspector.HasAttribute<Column.PrimaryKeyAttribute>())
                {
                    schemaField.PrimaryKey = true;
                    schemaField.AutoIncrement = fieldInspector.GetAttribute<Column.PrimaryKeyAttribute>().AutoIncrement;
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

                _fields[schemaField.FieldName] = schemaField;
                _mappedFields[schemaField.MappedName] = schemaField;

                fieldList.Add(schemaField);
            }

            _fieldList = fieldList.ToArray();

            _primaryKeys = _fieldList.Where(f => f.PrimaryKey).ToArray();
            _incrementKeys = _fieldList.Where(f => f.AutoIncrement).ToArray();

            /*
            if (_primaryKeys.Length == 0) // add primary key based on convention naming
            {
                var pkField = _fields[t.Name + "ID"] ?? _fields[t.Name + "Id"];

                if (pkField != null)
                {
                    pkField.PrimaryKey = true;
                    pkField.AutoIncrement = true;
                    _primaryKeys = new[] {pkField};
                    _incrementKeys = new[] {pkField};
                }
            }
             * */
        }

        private SafeDictionary<string,Relation> FindRelations()
        {
            var relations = new SafeDictionary<string, Relation>();

            foreach (var field in ObjectType.Inspector().GetFieldsAndProperties(BindingFlags.Instance | BindingFlags.Public).Where(f => !_mappableTypes.Contains(f.FieldType.Inspector().RealType)))
            {
                Type collectionType = field.FieldType.Inspector().GetInterfaces().FirstOrDefault(tI => tI.IsConstructedGenericType && tI.GetGenericTypeDefinition() == typeof (IEnumerable<>));
                
                if (field.Inspector().HasAttribute<Column.NotMappedAttribute>(false))
                    continue;

                if (collectionType != null)
                {
                    if (PrimaryKeys.Length != 1)
                        continue;

                    bool isDataSet = field.FieldType.IsConstructedGenericType && field.FieldType.GetGenericTypeDefinition() == typeof (IDataSet<>);

                    Type elementType = collectionType.GenericTypeArguments[0];

                    var foreignSchema = Repository.Context.GetSchema(elementType);

                    if (foreignSchema == null)
                        continue;

                    var localField = PrimaryKeys[0];
                    var foreignField = foreignSchema.Fields[localField.FieldName];

                    relations[field.Name] = new Relation(field)
                    {
                        RelationType = RelationType.OneToMany,
                        ForeignSchema = foreignSchema,
                        LocalSchema = this,
                        LocalField = localField,
                        ForeignField = foreignField,
                        ElementType = elementType,
                        IsDataSet = isDataSet
                    };
                }
                else
                {
                    Type objectType = field.FieldType;

                    var foreignSchema = Repository.Context.GetSchema(objectType);

                    var relationAttribute = field.Inspector().GetAttribute<DB.Relation.ManyToOneAttribute>(false) ?? new DB.Relation.ManyToOneAttribute();

                    if (foreignSchema == null || (foreignSchema.PrimaryKeys.Length != 1 && relationAttribute.ForeignKey == null))
                        continue;

                    var foreignField = (relationAttribute.ForeignKey != null) ? foreignSchema.Fields[relationAttribute.ForeignKey] : foreignSchema.PrimaryKeys[0];
                    var localField = (relationAttribute.LocalKey != null) ? Fields[relationAttribute.LocalKey] : Fields[foreignField.FieldName];

                    if (localField == null || foreignField == null)
                        continue; // TODO: throw exception

                    relations[field.Name] = new Relation(field)
                    {
                        ReadOnly = relationAttribute.ReadOnly,
                        RelationType = RelationType.ManyToOne,
                        ForeignSchema = foreignSchema,
                        LocalSchema = this,
                        LocalField = localField,
                        ForeignField = foreignField
                    };

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

        //private static readonly SafeDictionary<Type, OrmSchema> _allSchemas = new SafeDictionary<Type, OrmSchema>();

#if DEBUG
        public override string ToString()
        {
            return "<" + ObjectType.Name + ">";
        }
#endif
    }
}