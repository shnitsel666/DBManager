namespace Database.DataManager
{    
    /// <summary>
    /// Attribute for sortable column in case of using SELECT FROM {N} TAKE {M}
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class OrderAttribute : Attribute
    {
    }
}
