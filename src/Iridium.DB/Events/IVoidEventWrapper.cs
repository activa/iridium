using System;

namespace Iridium.DB
{
    public interface IVoidEventWrapper<out T>
    {
        void Add(Action<T> handler);
        void Remove(Action<T> handler);
    }
}