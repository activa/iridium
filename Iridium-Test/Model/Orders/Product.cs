namespace Iridium.DB.Test
{
    public class Product
    {
        // This will be a primary key by convention
        public string ProductID;

        [Column.Size(200)][Column.NotNull]
        public string Description;

        [Column.Size(10,Scale = 2)]
        public decimal Price;

        public int MinQty;
    }
}