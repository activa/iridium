#region License
//=============================================================================
// Velox.DB - Portable .NET ORM 
//
// Copyright (c) 2015 Philippe Leybaert
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

namespace Velox.DB
{
    public sealed class Field
    {
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class Composition : Attribute
        {
        }
    }

    public sealed class Column
    {
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class NameAttribute : Attribute
        {
            public string Name { get; private set; }

            public NameAttribute(string name)
            {
                Name = name;
            }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class IgnoreAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class IndexedAttribute : Attribute
        {
            public string IndexName { get; private set; }
            public int Position { get; set; }
            public bool Descending { get; set; }

            public IndexedAttribute()
            {
            }

            public IndexedAttribute(string indexName)
            {
                IndexName = indexName;
            }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class NotNullAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class NullAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class PrimaryKeyAttribute : Attribute
        {
            public bool AutoIncrement { get; set; }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class SizeAttribute : Attribute
        {
            public int Size { get; set; }
            public int Scale { get; set; }

            public SizeAttribute(int size)
            {
                Size = size;
            }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class LargeTextAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class ReadbackAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class NullValueAttribute : Attribute
        {
            public object NullValue { get; set; }

            public NullValueAttribute(object value)
            {
                NullValue = value;
            }
        }

        public class ForeignKeyAttribute : Attribute
        {
            public Type RelatedClass { get; set; }

            public ForeignKeyAttribute(Type relatedClass)
            {
                RelatedClass = relatedClass;
            }
        }
    }
}