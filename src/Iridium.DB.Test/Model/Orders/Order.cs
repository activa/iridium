using System;
using System.Collections.Generic;

namespace Iridium.DB.Test
{
    public interface IOrder
    {
        Customer Customer { get; set; }
    }

    public class Order : IOrder
    {
        public Order()
        {
            OrderDate = DateTime.Now;
        }

        public int OrderID { get; set; }
        public int CustomerID { get; set; }

        public int? SalesPersonID { get;set; }

        [Column.Name("Date")]
        public DateTime OrderDate { get; set; }

        public string Remark { get; set; }

        [Relation(LocalKey = "SalesPersonID")]
        public SalesPerson SalesPerson { get; set; }
        
        [Relation]
        public Customer Customer { get; set; }

        [Relation]
        public List<OrderItem> OrderItems { get; set; }
    }
}