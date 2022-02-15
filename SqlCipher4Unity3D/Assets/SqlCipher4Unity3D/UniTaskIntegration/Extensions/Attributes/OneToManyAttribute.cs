namespace SqlCipher4Unity3D.UniTaskIntegration.Extensions.Attributes
{
    public class OneToManyAttribute : RelationshipAttribute
    {
        public OneToManyAttribute(string inverseForeignKey = null, string inverseProperty = null)
            : base(null, inverseForeignKey, inverseProperty)
        {
        }
    }
}