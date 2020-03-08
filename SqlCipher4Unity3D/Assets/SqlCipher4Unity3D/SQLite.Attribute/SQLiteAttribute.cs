using System;

namespace SQLite.Attribute
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportAttribute : System.Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : System.Attribute
    {
        public TableAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : System.Attribute
    {
        public ColumnAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : System.Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class AutoIncrementAttribute : System.Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IndexedAttribute : System.Attribute
    {
        public IndexedAttribute()
        {
        }

        public IndexedAttribute(string name, int order)
        {
            this.Name = name;
            this.Order = order;
        }

        public string Name { get; set; }
        public int Order { get; set; }
        public virtual bool Unique { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : System.Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class UniqueAttribute : IndexedAttribute
    {
        public override bool Unique => true;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class MaxLengthAttribute : System.Attribute
    {
        public MaxLengthAttribute(int length)
        {
            this.Value = length;
        }

        public int Value { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CollationAttribute : System.Attribute
    {
        public CollationAttribute(string collation)
        {
            this.Value = collation;
        }

        public string Value { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class NotNullAttribute : System.Attribute
    {
    }
}