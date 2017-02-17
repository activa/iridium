namespace Iridium.DB.Test
{
    public class OneToOneRec2
    {
        [Column.PrimaryKeyAttribute(AutoIncrement = true)]
        public int OneToOneRec2ID;

        public int OneToOneRec1ID;

        [Relation.OneToOne] public OneToOneRec1 Rec1;
    }
}