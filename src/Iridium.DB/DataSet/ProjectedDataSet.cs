using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Iridium.DB
{
    internal class ProjectedDataSet<T,TSource> : IProjectedDataSet<T, TSource>
    {
        private readonly IDataSet<TSource> _dataSet;
        private readonly Expression<Func<TSource, T>> _selector;
        private bool _distinct;

        public ProjectedDataSet(IDataSet<TSource> dataSet, Expression<Func<TSource, T>> selector, bool distinct = false)
        {
            _dataSet = dataSet;
            _selector = selector;
            _distinct = distinct;
        }

        public ProjectedDataSet(ProjectedDataSet<T,TSource> dataSet, bool distinct = false)
        {
            _dataSet = dataSet._dataSet;
            _selector = dataSet._selector;
            _distinct = distinct;
        }

        public IProjectedDataSet<T, TSource> Distinct()
        {
            return new ProjectedDataSet<T, TSource>(_dataSet, _selector, true);
        }

        public IEnumerator<T> GetEnumerator()
        {
            var dataSet = _dataSet;

            if (_distinct)
                dataSet = dataSet.Distinct();

            return dataSet.Select(_selector.Compile()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}