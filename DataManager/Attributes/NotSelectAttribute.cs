namespace Database.DataManager
{
    /// <summary>
    /// No selectable field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NotSelectAttribute : Attribute
    {
    }
}
