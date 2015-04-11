using System;

namespace Velox.DB
{
    public class QuerySpec
    {
        public QuerySpec(ICodeQuerySpec code, INativeQuerySpec native)
        {
            Code = code;
            Native = native;
        }

        public ICodeQuerySpec Code { get; private set; }
        public INativeQuerySpec Native { get; private set; }
    }
}