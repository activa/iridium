using NUnit.Framework;

namespace Iridium.DB.Test
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
            DbContext.Instance = null;

            DB = MyContext.Get(driver);

            DbContext.Instance = DB;
        }
    }
}