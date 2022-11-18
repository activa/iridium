using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Iridium.DB
{
    public interface IAsyncProjectedDataSet<T, TSource>
    {
        IAsyncProjectedDataSet<T, TSource> Distinct();

        Task<T> ElementAt(int index);
        Task<List<T>> ToList();
        Task<T[]> ToArray();
        Task<Dictionary<TKey, T>> ToDictionary<TKey>(Func<T, TKey> keySelector);
        Task<Dictionary<TKey, T>> ToDictionary<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer);
        Task<Dictionary<TKey, TValue>> ToDictionary<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector);
        Task<Dictionary<TKey, TValue>> ToDictionary<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector, IEqualityComparer<TKey> comparer);
        Task<ILookup<TKey, T>> ToLookup<TKey>(Func<T, TKey> keySelector);
        Task<ILookup<TKey, T>> ToLookup<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer);
        Task<ILookup<TKey, TValue>> ToLookup<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector);
        Task<ILookup<TKey, TValue>> ToLookup<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector, IEqualityComparer<TKey> comparer);

        Task<T> First();
        Task<T> First(Func<T, bool> filter);
        Task<T> FirstOrDefault();
        Task<T> FirstOrDefault(Func<T, bool> filter);

        Task<bool> Any(Func<T, bool> filter);
        Task<bool> All(Func<T, bool> filter);
        Task<bool> Any();

        Task<int> Count();
        Task<int> Count(Func<T, bool> filter);
    }
}