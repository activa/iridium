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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Iridium.Reflection;

namespace Iridium.DB
{
    public partial class TableSchema
    {
        [Flags]
        public enum FieldFlags
        {
            PrimaryKey = 1<<1,
            AutoIncrement = 1<<2,
            Nullable = 1<<3,
            ReadOnly = 1<<4,
        }

        public class FieldOrRelation
        {
            public readonly MemberInfo FieldInfo;
            public readonly string FieldName;
            public readonly Type FieldType;

            protected FieldOrRelation(MemberInfo fieldInfo)
            {
                FieldInfo = fieldInfo;
                FieldName = fieldInfo.Name;
                FieldType = fieldInfo.Inspector().Type;
            }

            public object SetField(object target, object value)
            {
                FieldInfo.Inspector().SetValue(target, value);

                return target;
            }

            public T SetField<T>(T target, object value)
            {
                FieldInfo.Inspector().SetValue(target, value);

                return target;
            }

            public object GetField(object target)
            {
                return FieldInfo.Inspector().GetValue(target);
            }
#if DEBUG
            public override string ToString()
            {
                return $"<Field:{FieldName}>";
            }
#endif
        }

        public class Field : FieldOrRelation
        {
            public readonly bool CanBeNull;

            public Field(MemberInfo fieldInfo) : base(fieldInfo)
            {
                MappedName = FieldName;

                CanBeNull = FieldInfo.Inspector().Type.Inspector().CanBeNull;

                if (CanBeNull)
                    Flags |= FieldFlags.Nullable;
            }

            public string MappedName;

            public int? ColumnSize;
            public int? ColumnScale;

            public FieldFlags Flags;

            public bool PrimaryKey => (Flags & FieldFlags.PrimaryKey) != 0;
            public bool AutoIncrement => (Flags & FieldFlags.AutoIncrement) != 0;
            public bool ColumnNullable => (Flags & FieldFlags.Nullable) != 0;
            public bool ColumnReadOnly => (Flags & FieldFlags.ReadOnly) != 0;

            public void UpdateFlags(FieldFlags flags, bool? state)
            {
                if (state == null)
                    return;

                if (state.Value)
                    Flags |= flags;
                else
                    Flags &= ~flags;
            }
        }

        public enum RelationType
        {
            OneToMany, ManyToOne, OneToOne
        }

        public class Relation : FieldOrRelation
        {
            public RelationType RelationType;
            public TableSchema ForeignSchema;
            public TableSchema LocalSchema;
            public Field ForeignField;
            public Field LocalField;
            public Relation ReverseRelation;

            public Type ElementType;

            public bool ReadOnly; // If true, setting a relation object will not update the key value(s)
            public bool IsDataSet;
            public bool Preload;

            internal Relation(MemberInfo fieldInfo) : base(fieldInfo)
            {
            }

            public bool IsToOne => RelationType == RelationType.ManyToOne || RelationType == RelationType.OneToOne;

            private object CreateCollection(IEnumerable objects)
            {
                Type genericTypeDefinition = FieldType.IsConstructedGenericType ? FieldType.GetGenericTypeDefinition() : null;

                if (genericTypeDefinition == typeof (ICollection<>) || genericTypeDefinition == typeof (IList<>) || genericTypeDefinition == typeof (List<>))
                {
                    var list = (IList) Activator.CreateInstance(typeof (List<>).MakeGenericType(ElementType));

                    foreach (var o in objects)
                        list.Add(o);

                    return list;
                }

                if (!FieldType.IsArray && genericTypeDefinition != typeof (IEnumerable<>))
                    throw new NotSupportedException($"Collection type {FieldType} not supported");

                var tempArray = objects.Cast<object>().ToArray();

                var newArray = Array.CreateInstance(ElementType, tempArray.Length);

                Array.Copy(tempArray, newArray, newArray.Length);

                return newArray;
            }

            internal object LoadRelation(object parentObject)
            {
                var localFieldValue = LocalField.GetField(parentObject);

                var foreignRepository = ForeignSchema.Repository;

                if (IsToOne)
                {
                    var serializedForeignObject = localFieldValue == null ? null : foreignRepository.DataProvider.ReadObject(new Dictionary<string, object> {{ForeignField.MappedName,localFieldValue}}, ForeignSchema);

                    var relationObject = serializedForeignObject != null ? Ir.WithLoadedRelations(ForeignSchema.UpdateObject(Activator.CreateInstance(FieldType), serializedForeignObject),ForeignSchema.DatasetRelations) : null;

                    if (ReverseRelation != null)
                        relationObject = ReverseRelation.SetField(relationObject, parentObject);

                    return relationObject;
                }

                if (RelationType == RelationType.OneToMany)
                {
                    var parameter = Expression.Parameter(ElementType, "x");

                    Expression exp1 = Expression.MakeMemberAccess(parameter, ForeignField.FieldInfo);
                    Expression exp2 = Expression.Constant(localFieldValue, LocalField.FieldType);

                    if (exp2.Type != exp1.Type)
                        exp2 = Expression.Convert(exp2, exp1.Type);

                    var filter = new FilterSpec(Expression.Lambda(Expression.Equal(exp1,exp2), parameter));

                    if (IsDataSet)
                    {
                        return Activator.CreateInstance(typeof (DataSet<,>).MakeGenericType(ElementType,ElementType), ForeignSchema.Repository, filter, this, parentObject);
                    }
                    else
                    {
                        return CreateCollection(foreignRepository.GetRelationObjects(foreignRepository.CreateQuerySpec(filter), this, parentObject));
                    }
                }

                return null;
            }

#if DEBUG
            public override string ToString()
            {
                return $"{RelationType}:{LocalSchema.ObjectType.Name}.{FieldName} ({LocalField.FieldName}->{ForeignField.FieldName})";
            }
#endif

        }

        public class Index
        {
            public string Name;
            public Tuple<Field,SortOrder>[] FieldsWithOrder;
            public bool Unique;
        }
    }
}
