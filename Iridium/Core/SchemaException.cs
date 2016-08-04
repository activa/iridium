using System;

namespace Iridium.DB
{
    public class SchemaException : Exception
    {
        public SchemaException(string msg) : base(msg)
        {
        }
    }
}