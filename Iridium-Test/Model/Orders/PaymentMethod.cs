using System.Collections.Generic;

namespace Iridium.DB.Test
{
    public class PaymentMethod
    {
        public int PaymentMethodID { get; set; }
        public string Name { get; set; }

        [Relation]
        public ICollection<CustomerPaymentMethodLink> LinkedCustomers { get; set; }
    }
}