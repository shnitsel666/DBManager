namespace DatabaseManager
{
    /// <summary>
    /// No insertable field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NotInsertAttribute : Attribute
    {
    }
}
