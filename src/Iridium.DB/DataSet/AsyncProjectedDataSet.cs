using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Iridium.DB
{
    internal class AsyncProjectedDataSet<T, TSource> : IAsyncProjectedDataSet<T, TSource>
    {
        private readonly ProjectedDataSet<T, TSource> _dataSet;

        public AsyncProjectedDataSet(ProjectedDataSet<T, TSource> dataSet)
        {
            _dataSet = new ProjectedDataSet<T, TSource>(dataSet);
        }

        public Task<T> ElementAt(int index)
        {
            return Task.Run(() => _dataSet.ElementAt(index));
        }

        public IAsyncProjectedDataSet<T, TSource> Distinct()
        {
            return new AsyncProjectedDataSet<T, TSource>((ProjectedDataSet<T, TSource>) _dataSet.Distinct());
        }

        public Task<List<T>> ToList() => _dataSet.ToListAsync();
        public Task<T[]> ToArray() => _dataSet.ToArrayAsync();

        public Task<Dictionary<TKey, T>> ToDictionary<TKey>(Func<T, TKey> keySelector)
        {
            return Task.Run(() => _dataSet.ToDictionary(keySelector));
        }

        public Task<Dictionary<TKey, T>> ToDictionary<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return Task.Run(() => _dataSet.ToDictionary(keySelector, comparer));
        }

        public Task<Dictionary<TKey, TValue>> ToDictionary<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        {
            return Task.Run(() => _dataSet.ToDictionary(keySelector, valueSelector));
        }

        public Task<Dictionary<TKey, TValue>> ToDictionary<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector, IEqualityComparer<TKey> comparer)
        {
            return Task.Run(() => _dataSet.ToDictionary(keySelector, valueSelector, comparer));
        }

        public Task<ILookup<TKey, T>> ToLookup<TKey>(Func<T, TKey> keySelector)
        {
            return Task.Run(() => _dataSet.ToLookup(keySelector));
        }

        public Task<ILookup<TKey, T>> ToLookup<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return Task.Run(() => _dataSet.ToLookup(keySelector, comparer));
        }

        public Task<ILookup<TKey, TValue>> ToLookup<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        {
            return Task.Run(() => _dataSet.ToLookup(keySelector, valueSelector));
        }

        public Task<ILookup<TKey, TValue>> ToLookup<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector, IEqualityComparer<TKey> comparer)
        {
            return Task.Run(() => _dataSet.ToLookup(keySelector, valueSelector, comparer));
        }

        public Task<T> First() => _dataSet.FirstAsync();
        public Task<T> First(Func<T, bool> filter) => _dataSet.FirstAsync(filter);
        public Task<T> FirstOrDefault() => _dataSet.FirstOrDefaultAsync();
        public Task<T> FirstOrDefault(Func<T, bool> filter) => _dataSet.FirstOrDefaultAsync(filter);
        public Task<bool> Any(Func<T, bool> filter) => _dataSet.AnyAsync(filter);
        public Task<bool> All(Func<T, bool> filter) => _dataSet.AllAsync(filter);
        public Task<bool> Any() => _dataSet.AnyAsync();
        public Task<int> Count() => _dataSet.CountAsync();
        public Task<int> Count(Func<T, bool> filter) => _dataSet.CountAsync(filter);
    }
}