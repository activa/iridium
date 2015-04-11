using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Velox.DB.Test
{
    public static class AssertHelper
    {
        public static void AssertSorting<T>(IEnumerable<T> list, Func<T, object> getValue, Func<object, ComparisonConstraint> constraint)
        {
            object prev = null;

            foreach (var item in list)
            {
                if (prev == null)
                {
                    prev = getValue(item);
                }
                else
                {
                    Assert.That(getValue(item), constraint(prev));

                    prev = getValue(item);
                }
            }
        }

        public static void AssertSorting<T>(IEnumerable<T> list, Func<T, object> getValue1, Func<object, ComparisonConstraint> constraint1, Func<T, object> getValue2, Func<object, ComparisonConstraint> constraint2)
        {
            object prev1 = null;
            object prev2 = null;

            foreach (var item in list)
            {
                if (prev1 == null)
                {
                    prev1 = getValue1(item);
                    prev2 = getValue2(item);
                }
                else
                {
                    Assert.That(getValue1(item), constraint1(prev1));

                    if (object.Equals(item, prev1))
                        Assert.That(getValue2(item), constraint2(prev2));

                    prev1 = getValue1(item);
                    prev2 = getValue2(item);
                }
            }
        }

    }
}