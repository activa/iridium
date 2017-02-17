using System.Collections.Generic;

namespace Iridium.DB.Test
{
    public class Customer : IEntity
    {
        public int CustomerID { get; set; }
        public string Name { get; set; }
        public int? Age;

        [Relation]
        public ICollection<CustomerPaymentMethodLink> LinkedPaymentMethods { get; set; }
		
        public IDataSet<Order> Orders { get; set; }
    }
}