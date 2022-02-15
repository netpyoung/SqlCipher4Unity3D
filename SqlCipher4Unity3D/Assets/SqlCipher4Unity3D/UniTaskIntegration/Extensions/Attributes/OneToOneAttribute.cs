namespace SqlCipher4Unity3D.SQLite.Attribute
{
    using UniTaskIntegration.Extensions.Attributes;

    public class OneToOneAttribute : RelationshipAttribute
    {
        public OneToOneAttribute(string foreignKey = null, string inverseProperty = null) 
            : base(foreignKey, null, inverseProperty)
        {
        }
    }
}