namespace DatabaseManager
{
    /// <summary>
    /// Marks colummn as a primary key
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
    }
}
