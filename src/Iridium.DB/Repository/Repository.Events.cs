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

        private readonly ObjectEvents<object> _events = new ObjectEvents<object>(null);

        private readonly ConcurrentDictionary<Type, object> _typedEventHandlers = new ConcurrentDictionary<Type, object>();

        internal IObjectEvents<T1> Events<T1>()
        {
            return (IObjectEvents<T1>) _typedEventHandlers.GetOrAdd(typeof(T1), t => new ObjectEvents<T1>(_events));
        }
    }
}
