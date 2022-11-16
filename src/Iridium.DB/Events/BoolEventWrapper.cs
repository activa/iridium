using System;
using System.Collections.Generic;
using System.Linq;

namespace Iridium.DB
{
    internal class BoolEventWrapper<T> : IBoolEventWrapper<T>
    {
        private List<Func<T, bool>> _handlers = null;

        public void Add(Func<T, bool> handler)
        {
            if (_handlers == null)
                _handlers = new List<Func<T, bool>>();

            _handlers.Add(handler);
        }

        public void Remove(Func<T, bool> handler)
        {
            if (_handlers == null)
                return;

            _handlers.Remove(handler);
        }

        public bool Fire(T obj)
        {
            if (_handlers != null)
                return _handlers.All(handler => handler(obj));

            return true;
        }
    }
}