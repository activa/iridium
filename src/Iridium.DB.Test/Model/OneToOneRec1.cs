using System;
using System.Collections.Generic;

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

    public class Parent
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int ParentId { get; set; }

        public string Name { get; set; }

        [Relation]
        public ICollection<Child> Children { get; set; }

        public ICollection<GrandChild> Children2 { get; set; }
    }

    public class Child
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int ChildId { get; set; }

        public string Name { get; set; }
        public int ParentId { get; set; }

        [Relation]
        public ICollection<GrandChild> Children { get; set; }

        public ICollection<GrandChild> Children2 { get; set; }
    }

    public class GrandChild
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int GrandChildId { get; set; }

        public int ChildId { get; set; }

        public string Name { get; set; }
    }

}