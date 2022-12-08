namespace Database.DataManager
{
    /// <summary>
    /// No insertable field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NotInsertAttribute : Attribute
    {
    }
}
