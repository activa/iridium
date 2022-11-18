using System;
using System.Collections.Generic;
using System.Linq;

namespace Iridium.DB
{
    public interface IProjectedDataSet<T, TSource> : IEnumerable<T>
    {
        public IProjectedDataSet<T, TSource> Distinct();
    }
}