namespace Velox.DB.Test
{
    public static class ActiveRecordExtensions
    {
        public static bool Save(this Order order, bool saveRelations = false) => Vx.DataSet<Order>().InsertOrUpdate(order, saveRelations);
        public static bool Save(this Customer customer, bool saveRelations = false) => Vx.DataSet<Customer>().InsertOrUpdate(customer, saveRelations);
        public static bool Save(this OrderItem orderItem, bool saveRelations = false) => Vx.DataSet<OrderItem>().InsertOrUpdate(orderItem, saveRelations);
        public static bool Save(this SalesPerson salesPerson, bool saveRelations = false) => Vx.DataSet<SalesPerson>().InsertOrUpdate(salesPerson, saveRelations);
    }
}