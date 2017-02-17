namespace Iridium.DB.Test
{
    public class OneToOneRec1
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int OneToOneRec1ID;

        public int OneToOneRec2ID;

        [Relation.OneToOne] public OneToOneRec2 Rec2;

    }
}