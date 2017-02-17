namespace Iridium.DB.Test
{
    public class CustomerPaymentMethodLink
    {
        public int CustomerID { get; set; }
        public long PaymentMethodID { get; set; }

        [Relation]
        public Customer Customer;

        [Relation]
        public PaymentMethod PaymentMethod;
    }
}