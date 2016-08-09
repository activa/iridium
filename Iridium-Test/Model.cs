using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Iridium.DB;


namespace Iridium.DB.Test
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
	    public Order()
	    {
	        OrderDate = DateTime.Now;
	    }

        public int OrderID { get; set; }
		public int CustomerID { get; set; }

        [Column.ForeignKey(typeof(SalesPerson))]
        public int? SalesPersonID { get;set; }

		[Column.Name("Date")]
		public DateTime OrderDate { get; set; }

		public string Remark { get; set; }

		[Relation(LocalKey = "SalesPersonID")]
        public SalesPerson SalesPerson { get; set; }
        
		public Customer Customer { get; set; }

		[Relation]
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
		
        [Relation]
        public Order Order { get; set; }

        [Relation]
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

        [Relation(ForeignKey = "SalesPersonID")]
        public IDataSet<Order> Orders { get; set; }
	}

	public class Customer : IEntity
	{
		public int CustomerID { get; set; }
		public string Name { get; set; }
	    public int? Age;

        [Relation]
		public ICollection<CustomerPaymentMethodLink> LinkedPaymentMethods { get; set; }
		
		public IDataSet<Order> Orders { get; set; }
 	}

	public class PaymentMethod
	{
        public int PaymentMethodID { get; set; }
		public string Name { get; set; }
		public int MonthlyCost { get; set; }

        [Relation]
		public ICollection<CustomerPaymentMethodLink> LinkedCustomers { get; set; }
	}

	public class CustomerPaymentMethodLink
	{
		public int CustomerID { get; set; }
		public long PaymentMethodID { get; set; }

        [Relation]
	    public Customer Customer;

        [Relation]
	    public PaymentMethod PaymentMethod;
	}

    public class OneToOneRec1
    {
        [Column.PrimaryKeyAttribute(AutoIncrement = true)]
        public int OneToOneRec1ID;

        public int OneToOneRec2ID;

        [Relation.OneToOne] public OneToOneRec2 Rec2;

    }

    public class OneToOneRec2
    {
        [Column.PrimaryKeyAttribute(AutoIncrement = true)]
        public int OneToOneRec2ID;

        public int OneToOneRec1ID;

        [Relation.OneToOne] public OneToOneRec1 Rec1;
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

        public bool BooleanField;
        public bool? BooleanFieldNullable;
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

        public byte[] BlobField;

        public DateTime DateTimeField = new DateTime(1970,1,1);
        public DateTime? DateTimeFieldNullable;

        public TestEnum EnumField;
        public TestEnum? EnumFieldNullable;

        public TestEnumWithZero EnumWithZeroField;
        public TestEnumWithZero? EnumWithZeroFieldNullable;

        public TestFlagsEnum FlagsEnumField;
        public TestFlagsEnum? FlagsEnumFieldNullable;

    }

    public class RecordWithCompositeKey
    {
        [Column.PrimaryKeyAttribute]
        public int Key1;
        [Column.PrimaryKeyAttribute]
        public int Key2;
        public string Name;
    }
}
