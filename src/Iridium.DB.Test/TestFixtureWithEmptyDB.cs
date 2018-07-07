using System;
using System.Runtime.Remoting;
using System.Threading;
using Iridium.DB;
using Iridium.DB.Test;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    public abstract class TestFixtureWithEmptyDB : TestFixture
    {
        protected TestFixtureWithEmptyDB(string driver) : base(driver)
        {
        }

        [SetUp]
        public void SetupTest()
        {
            DB.PurgeAll();
        }
    }
}