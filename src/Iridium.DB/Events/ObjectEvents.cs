namespace Iridium.DB
{
    internal class ObjectEvents<T> : IObjectEvents<T>
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

        public ObjectEvents(ObjectEvents<object> parent)
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
}