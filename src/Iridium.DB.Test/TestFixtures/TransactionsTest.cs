using System;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("memory", Category = "memory")]
    [TestFixture("sqlitemem", Category = "sqlite-mem")]
    [TestFixture("sqlserver", Category = "sqlserver")]
    [TestFixture("sqlite", Category = "sqlite")]
    [TestFixture("mysql", Category = "mysql")]
    [TestFixture("postgres", Category = "postgres")]
    public class TransactionsTest : TestFixtureWithEmptyDB
    {
        public TransactionsTest(string driver) : base(driver)
        {
        }

        [Test]
        public void TransactionRollback()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));

            using (var transaction = new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });

                transaction.Rollback();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(0));
        }

        [Test]
        public void TransactionImplicitRollback()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));

            using (new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(0));
        }

        [Test]
        public void TransactionCommit()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));

            using (var transaction = new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });

                transaction.Commit();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Transaction_Implicit_Commit()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));

            using (new Transaction(DB ,commitOnDispose:true))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Transaction_Implicit_Commit_Rollback()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));

            using (var transaction = new Transaction(DB, commitOnDispose:true))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });

                transaction.Rollback();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(0));
        }



        [Test]
        public void NestedTransactions()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));
            //DB.Products.Count().Should().Be(0);

            using (var transaction1 = new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });

                using (var transaction2 = new Transaction(DB))
                {
                    DB.Products.Insert(new Product() { ProductID = "Y", Description = "Y" });

                    transaction2.Rollback();
                }

                transaction1.Commit();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(1));
            Assert.That(DB.Products.First().ProductID, Is.EqualTo("X"));
        }

        [Test]
        public void NestedTransactions2()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));

            using (var transaction1 = new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });

                using (var transaction2 = new Transaction(DB))
                {
                    DB.Products.Insert(new Product() { ProductID = "Y", Description = "Y" });

                    using (var transaction3 = new Transaction(DB))
                    {
                        DB.Products.Insert(new Product() { ProductID = "Z", Description = "Z" });

                        transaction3.Commit();
                    }

                    transaction2.Rollback();
                }

                transaction1.Commit();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(1));
            Assert.That(DB.Products.First().ProductID, Is.EqualTo("X"));
        }

        [Test]
        public void NestedTransactions3()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));

            using (var transaction1 = new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });

                using (var transaction2 = new Transaction(DB))
                {
                    DB.Products.Insert(new Product() { ProductID = "Y", Description = "Y" });

                    using (var transaction3 = new Transaction(DB))
                    {
                        DB.Products.Insert(new Product() { ProductID = "Z", Description = "Z" });

                        transaction3.Rollback();
                    }

                    transaction2.Commit();
                }

                transaction1.Commit();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(2));
            Assert.That(DB.Products.OrderBy(p => p.ProductID).First().ProductID, Is.EqualTo("X"));
            Assert.That(DB.Products.OrderBy(p => p.ProductID).Skip(1).First().ProductID, Is.EqualTo("Y"));
        }

        [Test]
        public void NestedTransactions4()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));

            using (var transaction1 = new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });

                using (var transaction2 = new Transaction(DB))
                {
                    DB.Products.Insert(new Product() { ProductID = "Y", Description = "Y" });

                    using (var transaction3 = new Transaction(DB))
                    {
                        DB.Products.Insert(new Product() { ProductID = "Z", Description = "Z" });

                        transaction3.Commit();
                    }

                    transaction2.Commit();
                }

                transaction1.Rollback();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(0));
        }

    }
}