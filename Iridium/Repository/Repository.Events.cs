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

        private class EventHandlers : Vx.IEvents<T>
        {
            public event EventHandler<Vx.ObjectWithCancelEventArgs<T>> ObjectCreating;
            public event EventHandler<Vx.ObjectEventArgs<T>> ObjectCreated;
            public event EventHandler<Vx.ObjectWithCancelEventArgs<T>> ObjectSaving;
            public event EventHandler<Vx.ObjectEventArgs<T>> ObjectSaved;
            public event EventHandler<Vx.ObjectWithCancelEventArgs<T>> ObjectDeleting;
            public event EventHandler<Vx.ObjectEventArgs<T>> ObjectDeleted;

            public void Fire_ObjectCreating(Vx.Context context, T obj, ref bool cancel)
            {
                if (ObjectCreating != null)
                {
                    var evArgs = new Vx.ObjectWithCancelEventArgs<T>(obj);

                    foreach (var @delegate in ObjectCreating.GetInvocationList())
                    {
                        var ev = (EventHandler<Vx.ObjectWithCancelEventArgs<T>>)@delegate;

                        ev(context, evArgs);

                        if (evArgs.Cancel)
                        {
                            cancel = evArgs.Cancel;
                            return;
                        }
                    }
                }
            }

            public void Fire_ObjectSaving(Vx.Context context, T obj, ref bool cancel)
            {
                if (ObjectSaving != null)
                {
                    var evArgs = new Vx.ObjectWithCancelEventArgs<T>(obj);

                    foreach (var @delegate in ObjectSaving.GetInvocationList())
                    {
                        var ev = (EventHandler<Vx.ObjectWithCancelEventArgs<T>>)@delegate;

                        ev(context, evArgs);

                        if (evArgs.Cancel)
                        {
                            cancel = evArgs.Cancel;
                            return;
                        }
                    }
                }
            }

            public void Fire_ObjectCreated(Vx.Context context, T obj)
            {
                ObjectCreated?.Invoke(context, new Vx.ObjectEventArgs<T>(obj));
            }

            public void Fire_ObjectSaved(Vx.Context context, T obj)
            {
                ObjectSaved?.Invoke(context, new Vx.ObjectEventArgs<T>(obj));
            }

            public void Fire_ObjectDeleting(Vx.Context context, T obj, ref bool cancel)
            {
                if (ObjectDeleting != null)
                {
                    var evArgs = new Vx.ObjectWithCancelEventArgs<T>(obj);

                    ObjectDeleting(context, evArgs);

                    cancel = evArgs.Cancel;
                }
            }

            public void Fire_ObjectDeleted(Vx.Context context, T obj)
            {
                ObjectDeleted?.Invoke(context, new Vx.ObjectEventArgs<T>(obj));
            }
        }

        private readonly EventHandlers _events = new EventHandlers();

        internal Vx.IEvents<T> Events => _events;
    }
}
