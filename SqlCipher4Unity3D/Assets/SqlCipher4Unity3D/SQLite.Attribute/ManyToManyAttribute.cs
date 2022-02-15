namespace SqlCipher4Unity3D.SQLite.Attribute
{
    using System;

    public class ManyToManyAttribute : RelationshipAttribute
    {
        public ManyToManyAttribute(Type intermediateType, string inverseForeignKey = null, string inverseProperty = null)
            : base(null, inverseForeignKey, inverseProperty)
        {
            IntermediateType = intermediateType;
        }

        public Type IntermediateType { get; private set; }
    }
}