namespace SqlCipher4Unity3D.SQLite.Attribute
{
    using System;
    using global::SQLite.Attributes;
    using SqlCipher4Unity3D.SQLite.Attribute;

    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : IndexedAttribute
    {
        public ForeignKeyAttribute(Type foreignType)
        {
            ForeignType = foreignType;
        }

        public Type ForeignType { get; private set; }
    }
}