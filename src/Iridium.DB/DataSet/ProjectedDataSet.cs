using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static Iridium.DB.TableSchema;

namespace Iridium.DB
{
    internal class ProjectedDataSet<T,TSource> : IProjectedDataSet<T, TSource>
    {
        private readonly IDataSet<TSource> _dataSet;
        private readonly Expression<Func<TSource, T>> _selector;

        public ProjectedDataSet(IDataSet<TSource> dataSet, Expression<Func<TSource, T>> selector)
        {
            _dataSet = dataSet;
            _selector = selector;
        }

        public ProjectedDataSet(ProjectedDataSet<T,TSource> dataSet)
        {
            _dataSet = dataSet._dataSet;
            _selector = dataSet._selector;
        }

        public IProjectedDataSet<T, TSource> Distinct()
        {
            return new ProjectedDataSet<T, TSource>(_dataSet.Distinct(), _selector);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _dataSet.Select(_selector.Compile()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IAsyncProjectedDataSet<T, TSource> Async() => new AsyncProjectedDataSet<T, TSource>(this);

        public Task<List<T>> ToListAsync() => Task.Run(this.ToList);
        public Task<T[]> ToArrayAsync() => Task.Run(this.ToArray);
        public Task<T> FirstAsync() => Task.Run(this.First);
        public Task<T> FirstAsync(Func<T, bool> filter) => Task.Run(() => this.First(filter));
        public Task<T> FirstOrDefaultAsync() => Task.Run(this.FirstOrDefault);
        public Task<T> FirstOrDefaultAsync(Func<T, bool> filter) => Task.Run(() => this.FirstOrDefault(filter));
        public Task<bool> AnyAsync(Func<T, bool> filter) => Task.Run(() => this.Any(filter));
        public Task<bool> AllAsync(Func<T, bool> filter) => Task.Run(() => this.All(filter));
        public Task<bool> AnyAsync() => Task.Run(this.Any);
        public Task<int> CountAsync() => Task.Run(this.Count);
        public Task<int> CountAsync(Func<T, bool> filter) => Task.Run(() => this.Count(filter));
    }
}