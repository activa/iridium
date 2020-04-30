using System;

namespace Iridium.DB.Test
{
    public class OneToOneRec1
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int OneToOneRec1ID;

        public int OneToOneRec2ID;

        [Relation.OneToOne] public OneToOneRec2 Rec2;

    }


    [Table.Name("B")]
    public class B
    {
        [Column.PrimaryKey(AutoIncrement = false)]
        public Guid Id { get; set; }
        [Column.Size(100)]
        public string Class { get; set; }
        [Relation.OneToOne(LocalKey ="Id", ForeignKey ="Id")]
        public A Other { get; set; }
    }

    [Table.Name("A")]
    public class A
    {
        [Column.PrimaryKey(AutoIncrement = false)]
        public Guid Id;
        [Column.Size(100)]
        public string MainNumber;
        [Relation.OneToOne(LocalKey ="Id", ForeignKey ="Id")]
        public B Other { get; set; }
    }
}