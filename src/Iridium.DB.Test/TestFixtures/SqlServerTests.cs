using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("sqlserver", Category = "sqlserver")]
    public class SqlServerTests : TestFixtureWithEmptyDB
    {
        public SqlServerTests(string driver) : base(driver)
        {
        }

        [Test]
        public void ExecStoredProcedureWithoutParameters()
        {
            try
            {
                DB.SqlNonQuery("DROP PROCEDURE AddCustomer");
            }
            catch
            {
            }

            DB.SqlNonQuery("CREATE PROCEDURE AddCustomer AS BEGIN INSERT INTO Customer (Name) VALUES ('TEST1') END");

            Assert.That(DB.Customers.Count(), Is.EqualTo(0));

            DB.SqlNonQuery("EXEC AddCustomer");

            Assert.That(DB.Customers.Count(), Is.EqualTo(1));
            Assert.That(DB.Customers.First().Name, Is.EqualTo("TEST1"));
        }

        [Test]
        public void StoredProcedureWithoutParameters()
        {
            try
            {
                DB.SqlNonQuery("DROP PROCEDURE AddCustomer");
            }
            catch
            {
            }

            DB.SqlNonQuery("CREATE PROCEDURE AddCustomer AS BEGIN INSERT INTO Customer (Name) VALUES ('TEST1') END");

            Assert.That(DB.Customers.Count(), Is.EqualTo(0));

            DB.SqlProcedure("AddCustomer");

            Assert.That(DB.Customers.Count(), Is.EqualTo(1));
            Assert.That(DB.Customers.First().Name, Is.EqualTo("TEST1"));
        }

        [Test]
        public void ExecStoredProcedureWithParameters()
        {
            try
            {
                DB.SqlNonQuery("DROP PROCEDURE AddCustomer");
            }
            catch
            {
            }

            DB.SqlNonQuery("CREATE PROCEDURE AddCustomer @name varchar(50) AS BEGIN INSERT INTO Customer (Name) VALUES (@name) END");

            Assert.That(DB.Customers.Count(), Is.EqualTo(0));

            DB.SqlNonQuery("EXEC AddCustomer @name=@n" , new { n = "NAME1"});

            Assert.That(DB.Customers.Count(), Is.EqualTo(1));
            Assert.That(DB.Customers.First().Name, Is.EqualTo("NAME1"));

            DB.SqlNonQuery("EXEC AddCustomer @name" , new { name = "NAME2"});

            Assert.That(DB.Customers.Count(), Is.EqualTo(2));
            Assert.That(DB.Customers.OrderBy(c => c.Name).First().Name, Is.EqualTo("NAME1"));
            Assert.That(DB.Customers.OrderBy(c => c.Name).Skip(1).First().Name, Is.EqualTo("NAME2"));

        }

        [Test]
        public void StoredProcedureWithParameters()
        {
            try
            {
                DB.SqlNonQuery("DROP PROCEDURE AddCustomer");
            }
            catch
            {
            }

            DB.SqlNonQuery("CREATE PROCEDURE AddCustomer @name varchar(50) AS BEGIN INSERT INTO Customer (Name) VALUES (@name) END");

            Assert.That(DB.Customers.Count(), Is.EqualTo(0));

            DB.SqlProcedure("AddCustomer" , new { name = "NAME1"});

            Assert.That(DB.Customers.Count(), Is.EqualTo(1));
            Assert.That(DB.Customers.First().Name, Is.EqualTo("NAME1"));

            DB.SqlProcedure("AddCustomer" , new { name = "NAME2"});

            Assert.That(DB.Customers.Count(), Is.EqualTo(2));
            Assert.That(DB.Customers.OrderBy(c => c.Name).First().Name, Is.EqualTo("NAME1"));
            Assert.That(DB.Customers.OrderBy(c => c.Name).Skip(1).First().Name, Is.EqualTo("NAME2"));
        }

    }
}