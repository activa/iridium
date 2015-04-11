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
        public class FieldOrRelation
        {
            public readonly FieldOrPropertyInfo Accessor;
            public readonly string FieldName;
            public readonly Type FieldType;

            public FieldOrRelation(FieldOrPropertyInfo accessor)
            {
                Accessor = accessor;
                FieldName = accessor.Name;
                FieldType = accessor.FieldType;
            }

            public object SetField(object target, object value)
            {
                Accessor.SetValue(target, value);

                return target;
            }

            public T SetField<T>(T target, object value)
            {
                Accessor.SetValue(target, value);

                return target;
            }

            public object GetField(object target)
            {
                return Accessor.GetValue(target);
            }

        }

        public class Field : FieldOrRelation
        {
            public readonly Type RealFieldType;
            public readonly bool CanBeNull;

            public Field(FieldOrPropertyInfo accessor) : base(accessor)
            {
                MappedName = FieldName;
                RealFieldType = FieldType.Inspector().RealType;

                CanBeNull = FieldType.Inspector().CanBeNull;
                ColumnNullable = CanBeNull;
            }

            public string MappedName;

            public bool PrimaryKey;
            public bool AutoIncrement;
            public int? ColumnSize;
            public int? ColumnScale;
            public bool ColumnNullable;
        }

        public enum RelationType
        {
            OneToMany, ManyToOne
        }

        public class Relation : FieldOrRelation
        {
            public RelationType RelationType;
            public OrmSchema ForeignSchema;
            public OrmSchema LocalSchema;
            public Field ForeignField;
            public Field LocalField;

            public Type ElementType;

            public bool ReadOnly; // If true, setting a relation object will not update the key value(s)
            public bool IsDataSet;

            internal Relation(FieldOrPropertyInfo accessor) : base(accessor)
            {
            }

            internal object CreateCollection(IEnumerable objects)
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
                    throw new NotSupportedException(string.Format("Collection type {0} not supported", FieldType));

                var tempArray = objects.Cast<object>().ToArray();

                var newArray = Array.CreateInstance(ElementType, tempArray.Length);

                Array.Copy(tempArray, newArray, newArray.Length);

                return newArray;
            }

            internal object LoadRelation(object parentObject)
            {
                Debug.WriteLine("Retrieving relation '{0}' from object {1}", FieldName, parentObject);

                var localFieldValue = LocalField.GetField(parentObject);

                var foreignRepository = ForeignSchema.Repository;

                if (RelationType == RelationType.ManyToOne)
                {
                    foreignRepository.IncrementQueryCount();

                    var serializedForeignObject = localFieldValue == null ? null : foreignRepository.DataProvider.ReadObject(new[] {localFieldValue}, ForeignSchema);

                    return serializedForeignObject != null ? ForeignSchema.UpdateObject(Activator.CreateInstance(FieldType), serializedForeignObject) : null;
                }

                if (RelationType == RelationType.OneToMany)
                {
                    var parameter = Expression.Parameter(ElementType, "x");

                    var lambda = Expression.Equal(
                        Expression.MakeMemberAccess(parameter, ForeignField.Accessor.AsMember),
                        Expression.Constant(localFieldValue)
                        );

                    var filter = new FilterSpec(Expression.Lambda(lambda, parameter));

                    if (IsDataSet)
                    {
                        return Activator.CreateInstance(typeof (DataSet<>).MakeGenericType(ElementType), ForeignSchema.Repository, filter);
                    }
                    else
                    {
                        return CreateCollection(foreignRepository.GetRelationObjects(foreignRepository.CreateQuerySpec(filter)));
                    }
                }

                return null;
            }

#if DEBUG
            public override string ToString()
            {
                return string.Format("{0}:{1}.{2} ({3}->{4})", RelationType, LocalSchema.ObjectType.Name, FieldName, LocalField.FieldName, ForeignField.FieldName);
            }
#endif

        }
    }
}
