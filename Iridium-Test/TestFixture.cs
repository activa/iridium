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

        protected TestFixture(string driver)
        {
            Driver = driver;

            StorageContext.Instance = null;

            DB = DBContext.Get(driver);

            StorageContext.Instance = DB;
        }

        protected T[] InsertRecords<T>(int n, Action<T, int> action) where T : new()
        {
            var list = new List<T>(n);

            for (int i = 1; i <= n; i++)
            {
                var rec = new T();

                action(rec, i);

                DB.Insert(rec);

                list.Add(rec);
            }

            Assert.That(DB.DataSet<T>().Count(), Is.EqualTo(n));

            return list.ToArray();
        }

        protected T InsertRecord<T>(T rec)
        {
            DB.Insert(rec);

            return rec;
        }


    }
}