using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace Velox.DB.Test
{
    public class Product
    {
        // This will be a primary key with autoincrement, by convention
        public string ProductID;

        [Column.Size(200)][Column.NotNull]
        public string Description;

        [Column.Size(10,Scale = 2)]
        public decimal Price;

        public int MinQty;
    }

	public class Order
	{
        public static Order Read(int id, params Expression<Func<Order, object>>[] relations)
        {
            return Vx.DataSet<Order>().Read(id, relations);
        }

	    public Order()
	    {
	        OrderDate = DateTime.Now;
	    }

        public int OrderID { get; set; }
		public int CustomerID { get; set; }
        public int ProductID { get; set; }

	    public Product Product;

        public int? SalesPersonID { get;set; }

		[Column.Name("Date")]
		public DateTime OrderDate { get; set; }

		public string Remark { get; set; }

		[Relation.ManyToOne(LocalKey = "SalesPersonID")]
        public SalesPerson SalesPerson { get; set; }
        
		public Customer Customer { get; set; }
		
        public ICollection<OrderItem> OrderItems { get; set; }
	}

	public class OrderItem
	{
        public int OrderItemID { get; set; }
		public int OrderID { get;set; }
		public short Qty { get; set; }
		public double Price { get; set; }
        [Column.Size(200)]
		public string Description { get; set; }
        public string ProductID { get; set; }
		
        public Order Order { get; set; }
	    public Product Product;
	}

	public enum SalesPersonType { Internal, External };

	public class SalesPerson
	{
        [Column.PrimaryKeyAttribute(AutoIncrement = true), Column.Name("SalesPersonID")]
        public int ID;

	    public string Name;
		public SalesPersonType? SalesPersonType { get; set; }
		public int? Test { get; set; }

        public IList<Order> Orders { get; set; }
	}

	public class Customer
	{
		public int CustomerID { get; set; }
		public string Name { get; set; }
	    public int? Age;

		public ICollection<CustomerPaymentMethodLink> LinkedPaymentMethods { get; set; }
		
		public IDataSet<Order> Orders { get; set; }
 	}

	public class PaymentMethod
	{
        public int PaymentMethodID { get; set; }
		public string Name { get; set; }
		public int MonthlyCost { get; set; }

		public ICollection<CustomerPaymentMethodLink> LinkedCustomers { get; set; }
	}

	public class CustomerPaymentMethodLink
	{
		public int CustomerID { get; set; }
		public long PaymentMethodID { get; set; }

	    public Customer Customer;
	    public PaymentMethod PaymentMethod;
	}

    public enum TestEnumWithZero
    {
        Zero = 0,
        One = 1,
        Two = 2
    }

    public enum TestEnum
    {
        One = 1,
        Two = 2
    }

    [Flags]
    public enum TestFlagsEnum
    {
        One = 1,
        Two = 2,
        Four = 4
    }

    public class RecordWithAllTypes
    {
        [Column.PrimaryKeyAttribute(AutoIncrement = true)]
        public int PK;

        public byte ByteField;
        public byte? ByteFieldNullable;
        public short ShortField;
        public short? ShortFieldNullable;
        public int IntField;
        public int? IntFieldNullable;
        public long LongField;
        public long? LongFieldNullable;
        public decimal DecimalField;
        public decimal? DecimalFieldNullable;
        public float FloatField;
        public float? FloatFieldNullable;
        public double DoubleField;
        public double DoubleFieldNullable;
        public string StringField;
        [Column.LargeText]
        public string LongStringField;

        public DateTime DateTimeField = DateTime.Now;
        public DateTime? DateTimeFieldNullable;

        public TestEnum EnumField;
        public TestEnum? EnumFieldNullable;

        public TestEnumWithZero EnumWithZeroField;
        public TestEnumWithZero? EnumWithZeroFieldNullable;

        public TestFlagsEnum FlagsEnumField;
        public TestFlagsEnum? FlagsEnumFieldNullable;

    }
}
