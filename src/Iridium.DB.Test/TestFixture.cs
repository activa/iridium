using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture]
    public abstract class TestFixture
    {
        public string Driver;

        public DBContext DB
        {
            get;
        }

        [OneTimeTearDown]
        public void CloseContext()
        {
            DB.Dispose();
        }

        protected TestFixture(string driver)
        {
            Driver = driver;

            DB = DBContext.GetContextFactory(driver)();

            DB.CreateAllTables();
            DB.PurgeAll();
            
        }

        protected T[] InsertRecords<T>(int n, Action<T, int> action) where T : new()
        {
            long prevCount = DB.DataSet<T>().Count();

            var list = new List<T>(n);

            for (int i = 1; i <= n; i++)
            {
                var rec = new T();

                action(rec, i);

                DB.Insert(rec);

                list.Add(rec);
            }

            Assert.That(DB.DataSet<T>().Count(), Is.EqualTo(n+prevCount));

            return list.ToArray();
        }

        protected T InsertRecord<T>(T rec)
        {
            DB.Insert(rec);

            return rec;
        }

        

    }
}