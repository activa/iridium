namespace Iridium.DB.Test
{
    public class SalesPerson
    {
        [Column.PrimaryKey(AutoIncrement = true), Column.Name("SalesPersonID")]
        public int ID;

        public string Name;
        public SalesPersonType? SalesPersonType { get; set; }
        public int? Test { get; set; }

        [Relation(ForeignKey = "SalesPersonID")]
        public IDataSet<Order> Orders { get; set; }
    }

    public enum SalesPersonType { Internal, External };
}