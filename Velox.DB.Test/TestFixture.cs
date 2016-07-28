using NUnit.Framework;

namespace Velox.DB.Test
{
    [TestFixture]
    public abstract class TestFixture
    {
        public MyContext DB
        {
            get;
        }

        protected TestFixture(string driver)
        {
            DB = MyContext.Get(driver);
            Vx.DB = DB;
        }
    }
}