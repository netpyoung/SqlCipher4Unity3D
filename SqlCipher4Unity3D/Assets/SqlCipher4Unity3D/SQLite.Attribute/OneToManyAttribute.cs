namespace SqlCipher4Unity3D.SQLite.Attribute
{
    public class OneToManyAttribute : RelationshipAttribute
    {
        public OneToManyAttribute(string inverseForeignKey = null, string inverseProperty = null)
            : base(null, inverseForeignKey, inverseProperty)
        {
        }
    }
}