using System;

namespace Iridium.DB
{
    public interface IBoolEventWrapper<out T>
    {
        void Add(Func<T, bool> handler);
        void Remove(Func<T, bool> handler);
    }
}