using System;
using System.Collections.Generic;

namespace Velox.DB
{
    public interface ICodeQuerySpec
    {
        bool IsFilterMatch(object o);
        object ExpressionValue(object o);
        int Compare(object o1, object o2);
        IEnumerable<T> Range<T>(IEnumerable<T> objects);
    }
}