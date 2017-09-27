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

namespace Iridium.DB
{
    internal partial class Repository<T>
    {
        public override void Fire_ObjectCreating(object obj, ref bool cancel) { _events.Fire_ObjectCreating(Context, (T)obj, ref cancel); }
        public override void Fire_ObjectCreated(object obj) { _events.Fire_ObjectCreated(Context, (T)obj); }
        public override void Fire_ObjectSaving(object obj, ref bool cancel) { _events.Fire_ObjectSaving(Context, (T)obj, ref cancel); }
        public override void Fire_ObjectSaved(object obj) { _events.Fire_ObjectSaved(Context, (T)obj); }
        public override void Fire_ObjectDeleting(object obj, ref bool cancel) { _events.Fire_ObjectDeleting(Context, (T)obj, ref cancel); }
        public override void Fire_ObjectDeleted(object obj) { _events.Fire_ObjectDeleted(Context, (T)obj); }

        private class EventHandlers : IObjectEvents<T>
        {
            public event EventHandler<ObjectWithCancelEventArgs<T>> ObjectCreating;
            public event EventHandler<ObjectEventArgs<T>> ObjectCreated;
            public event EventHandler<ObjectWithCancelEventArgs<T>> ObjectSaving;
            public event EventHandler<ObjectEventArgs<T>> ObjectSaved;
            public event EventHandler<ObjectWithCancelEventArgs<T>> ObjectDeleting;
            public event EventHandler<ObjectEventArgs<T>> ObjectDeleted;

            public void Fire_ObjectCreating(StorageContext context, T obj, ref bool cancel)
            {
                if (ObjectCreating != null)
                {
                    var evArgs = new ObjectWithCancelEventArgs<T>(obj);

                    foreach (var @delegate in ObjectCreating.GetInvocationList())
                    {
                        var ev = (EventHandler<ObjectWithCancelEventArgs<T>>)@delegate;

                        ev(context, evArgs);

                        if (evArgs.Cancel)
                        {
                            cancel = evArgs.Cancel;
                            return;
                        }
                    }
                }
            }

            public void Fire_ObjectSaving(StorageContext context, T obj, ref bool cancel)
            {
                if (ObjectSaving != null)
                {
                    var evArgs = new ObjectWithCancelEventArgs<T>(obj);

                    foreach (var @delegate in ObjectSaving.GetInvocationList())
                    {
                        var ev = (EventHandler<ObjectWithCancelEventArgs<T>>)@delegate;

                        ev(context, evArgs);

                        if (evArgs.Cancel)
                        {
                            cancel = evArgs.Cancel;
                            return;
                        }
                    }
                }
            }

            public void Fire_ObjectCreated(StorageContext context, T obj)
            {
                ObjectCreated?.Invoke(context, new ObjectEventArgs<T>(obj));
            }

            public void Fire_ObjectSaved(StorageContext context, T obj)
            {
                ObjectSaved?.Invoke(context, new ObjectEventArgs<T>(obj));
            }

            public void Fire_ObjectDeleting(StorageContext context, T obj, ref bool cancel)
            {
                if (ObjectDeleting != null)
                {
                    var evArgs = new ObjectWithCancelEventArgs<T>(obj);

                    ObjectDeleting(context, evArgs);

                    cancel = evArgs.Cancel;
                }
            }

            public void Fire_ObjectDeleted(StorageContext context, T obj)
            {
                ObjectDeleted?.Invoke(context, new ObjectEventArgs<T>(obj));
            }
        }

        private readonly EventHandlers _events = new EventHandlers();

        internal IObjectEvents<T> Events => _events;
    }
}
