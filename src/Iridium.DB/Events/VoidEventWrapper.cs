using System;
using System.Collections.Generic;

namespace Iridium.DB
{
    internal class VoidEventWrapper<T> : IVoidEventWrapper<T>
    {
        private List<Action<T>> _handlers = null;

        public void Add(Action<T> handler)
        {
            if (_handlers == null)
                _handlers = new List<Action<T>>();

            _handlers.Add(handler);
        }

        public void Remove(Action<T> handler)
        {
            if (_handlers == null)
                return;

            _handlers.Remove(handler);
        }

        public void Fire(T obj)
        {
            if (_handlers != null)
                foreach (var function in _handlers)
                    function(obj);
        }


    }
}