namespace Iridium.DB.Test
{
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
}