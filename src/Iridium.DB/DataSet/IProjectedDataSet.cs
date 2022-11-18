using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Iridium.DB
{
    public interface IProjectedDataSet<T, TSource> : IEnumerable<T>
    {
        IProjectedDataSet<T, TSource> Distinct();
        IAsyncProjectedDataSet<T, TSource> Async();

        Task<List<T>> ToListAsync();
        Task<T[]> ToArrayAsync();

        Task<T> FirstAsync();
        Task<T> FirstAsync(Func<T, bool> filter);
        Task<T> FirstOrDefaultAsync();
        Task<T> FirstOrDefaultAsync(Func<T, bool> filter);

        Task<bool> AnyAsync(Func<T, bool> filter);
        Task<bool> AllAsync(Func<T, bool> filter);
        Task<bool> AnyAsync();

        Task<int> CountAsync();
        Task<int> CountAsync(Func<T, bool> filter);
    }
}