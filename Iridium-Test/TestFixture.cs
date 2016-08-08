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
            StorageContext.Instance = null;

            DB = MyContext.Get(driver);

            StorageContext.Instance = DB;
        }
    }
}