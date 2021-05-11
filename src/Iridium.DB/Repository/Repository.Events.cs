#region License
//=============================================================================
// Iridium - Porable .NET ORM 
//
// Copyright (c) 2015-2017 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Iridium.DB
{
    internal partial class Repository<T>
    {
        public override bool Fire_ObjectCreating(object obj) => _events.Fire_ObjectCreating(obj);
        public override void Fire_ObjectCreated(object obj) => _events.Fire_ObjectCreated(obj);
        public override bool Fire_ObjectSaving(object obj) => _events.Fire_ObjectSaving(obj);
        public override void Fire_ObjectSaved(object obj) => _events.Fire_ObjectSaved(obj);
        public override bool Fire_ObjectDeleting(object obj) => _events.Fire_ObjectDeleting(obj);
        public override void Fire_ObjectDeleted(object obj) => _events.Fire_ObjectDeleted(obj);
        public override void Fire_ObjectRead(object obj) => _events.Fire_ObjectRead(obj);

        private readonly EventHandlers<object> _events = new EventHandlers<object>(null);

        private readonly ConcurrentDictionary<Type, object> _typedEventHandlers = new ConcurrentDictionary<Type, object>();

        internal IObjectEvents<T1> Events<T1>()
        {
            return (IObjectEvents<T1>) _typedEventHandlers.GetOrAdd(typeof(T1), t => new EventHandlers<T1>(_events));
        }
    }

    
    internal class EventHandlers<T> : IObjectEvents<T>
    {
        private readonly VoidEventWrapper<T> _created = new VoidEventWrapper<T>();
        private readonly VoidEventWrapper<T> _saved = new VoidEventWrapper<T>();
        private readonly VoidEventWrapper<T> _deleted = new VoidEventWrapper<T>();
        private readonly VoidEventWrapper<T> _read = new VoidEventWrapper<T>();

        private readonly BoolEventWrapper<T> _creating = new BoolEventWrapper<T>();
        private readonly BoolEventWrapper<T> _saving = new BoolEventWrapper<T>();
        private readonly BoolEventWrapper<T> _deleting = new BoolEventWrapper<T>();

        public IVoidEventWrapper<T> Created => _created;
        public IVoidEventWrapper<T> Saved => _saved;
        public IVoidEventWrapper<T> Deleted => _deleted;
        public IVoidEventWrapper<T> Read => _read;
        public IBoolEventWrapper<T> Creating => _creating;
        public IBoolEventWrapper<T> Saving => _saving;
        public IBoolEventWrapper<T> Deleting => _deleting;

        public EventHandlers(EventHandlers<object> parent)
        {
            if (parent != null)
            {
                parent._created.Add(o => _created.Fire((T) o));
                parent._saved.Add(o => _created.Fire((T) o));
                parent._deleted.Add(o => _created.Fire((T) o));
                parent._read.Add(o => _created.Fire((T) o));

                parent._creating.Add(o => _creating.Fire((T) o));
                parent._saving.Add(o => _creating.Fire((T) o));
                parent._deleting.Add(o => _creating.Fire((T) o));
            }
        }

        public bool Fire_ObjectCreating(T obj) => _creating.Fire(obj);
        public bool Fire_ObjectSaving(T obj) => _saving.Fire(obj);
        public bool Fire_ObjectDeleting(T obj) => _deleting.Fire(obj);
        public void Fire_ObjectCreated(T obj) => _created.Fire(obj);
        public void Fire_ObjectSaved(T obj) => _saved.Fire(obj);
        public void Fire_ObjectDeleted(T obj) => _deleted.Fire(obj);
        public void Fire_ObjectRead(T obj) => _read.Fire(obj);
    }

    public interface IBoolEventWrapper<T>
    {
        void Add(Func<T, bool> handler);
        void Remove(Func<T, bool> handler);
    }

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

    public interface IVoidEventWrapper<T>
    {
        void Add(Action<T> handler);
        void Remove(Action<T> handler);
    }

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
