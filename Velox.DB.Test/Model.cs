using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Velox.DB.Test
{
    public class Product
    {
        [Column.PrimaryKey]
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

		[MapTo("Date")]
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
        [Column.PrimaryKeyAttribute(AutoIncrement = true)][MapTo("SalesPersonID")]
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
}
