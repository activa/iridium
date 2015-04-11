namespace Velox.DB.Test
{
    public static class ActiveRecordExtensions
    {
        public static bool Save(this Order order, bool saveRelations = false)
        {
            return Vx.DataSet<Order>().Save(order, saveRelations);
        }
        public static bool Save(this Customer customer, bool saveRelations = false)
        {
            return Vx.DataSet<Customer>().Save(customer, saveRelations);
        }
        public static bool Save(this OrderItem orderItem, bool saveRelations = false)
        {
            return Vx.DataSet<OrderItem>().Save(orderItem, saveRelations);
        }
        public static bool Save(this SalesPerson salesPerson, bool saveRelations = false)
        {
            return Vx.DataSet<SalesPerson>().Save(salesPerson, saveRelations);
        }
        
    }
}