using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("postgres", Category = "server")]
    public class PostgresTests : TestFixtureWithEmptyDB
    {
        public PostgresTests(string driver) : base(driver)
        {
        }

        [Test]
        public void ExecStoredProcedureWithoutParameters()
        {
            DB.SqlNonQuery("DROP FUNCTION IF EXISTS AddCustomer ()");

            DB.SqlNonQuery("CREATE FUNCTION AddCustomer () RETURNS void AS $$ BEGIN INSERT INTO \"Customer\" (\"Name\") VALUES ('TEST1'); END; $$ LANGUAGE plpgsql");

            Assert.That(DB.Customers.Count(), Is.EqualTo(0));

            DB.SqlNonQuery("select AddCustomer()");

            Assert.That(DB.Customers.Count(), Is.EqualTo(1));
            Assert.That(DB.Customers.First().Name, Is.EqualTo("TEST1"));
        }

        [Test]
        public void StoredProcedureWithoutParameters()
        {
            DB.SqlNonQuery("DROP FUNCTION IF EXISTS AddCustomer ()");

            DB.SqlNonQuery("CREATE FUNCTION AddCustomer () RETURNS void AS $$ BEGIN INSERT INTO \"Customer\" (\"Name\") VALUES ('TEST1'); END; $$ LANGUAGE plpgsql");

            Assert.That(DB.Customers.Count(), Is.EqualTo(0));

            DB.SqlProcedure("AddCustomer");

            Assert.That(DB.Customers.Count(), Is.EqualTo(1));
            Assert.That(DB.Customers.First().Name, Is.EqualTo("TEST1"));
        }

        [Test]
        public void ExecStoredProcedureWithParameters()
        {
            DB.SqlNonQuery("DROP FUNCTION IF EXISTS AddCustomer (varchar(50))");

            DB.SqlNonQuery("CREATE FUNCTION AddCustomer (name varchar(50)) RETURNS void AS $$ BEGIN INSERT INTO \"Customer\" (\"Name\") VALUES (name); END; $$ LANGUAGE plpgsql");

            Assert.That(DB.Customers.Count(), Is.EqualTo(0));

            DB.SqlNonQuery("select AddCustomer (@n)" , new { n = "NAME1"});

            Assert.That(DB.Customers.Count(), Is.EqualTo(1));
            Assert.That(DB.Customers.First().Name, Is.EqualTo("NAME1"));

            DB.SqlNonQuery("select AddCustomer (@name)" , new { name = "NAME2"});

            Assert.That(DB.Customers.Count(), Is.EqualTo(2));
            Assert.That(DB.Customers.OrderBy(c => c.Name).First().Name, Is.EqualTo("NAME1"));
            Assert.That(DB.Customers.OrderBy(c => c.Name).Skip(1).First().Name, Is.EqualTo("NAME2"));

        }

        [Test]
        public void StoredProcedureWithParameters()
        {
            DB.SqlNonQuery("DROP FUNCTION IF EXISTS AddCustomer (varchar(50))");

            DB.SqlNonQuery("CREATE FUNCTION AddCustomer (name varchar(50)) RETURNS void AS $$ BEGIN INSERT INTO \"Customer\" (\"Name\") VALUES (name); END; $$ LANGUAGE plpgsql");

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