namespace SqlCipher4Unity3D.UniTaskIntegration.Extensions.Attributes
{
    using System;
    using global::SQLite.Attributes;

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