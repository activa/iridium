using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Iridium.DB.Test
{
    public static class AssertHelper
    {
        public static void AssertSorting<T>(IEnumerable<T> list, Func<T,T, bool> getValue) where T:class
        {
            T prev = null;

            foreach (var item in list)
            {
                if (prev == null)
                {
                    prev = item;
                }
                else
                {
                    getValue(prev, item).Should().BeTrue();

                    prev = item;
                }
            }
        }

        /*
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
        }*/

    }
}