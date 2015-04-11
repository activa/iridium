using System;

namespace Velox.DB
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class)]
    public sealed class MapToAttribute : Attribute
    {
        public string Name { get; private set; }

        public MapToAttribute(string name)
        {
            Name = name;
        }
    }
}