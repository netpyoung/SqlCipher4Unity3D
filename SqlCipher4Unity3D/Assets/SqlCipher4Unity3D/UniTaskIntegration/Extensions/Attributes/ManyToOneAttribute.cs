namespace SqlCipher4Unity3D.UniTaskIntegration.Extensions.Attributes
{
    public class ManyToOneAttribute : RelationshipAttribute
    {
        public ManyToOneAttribute(string foreignKey = null, string inverseProperty = null)
            : base(foreignKey, null, inverseProperty)
        {
        }

    }
}