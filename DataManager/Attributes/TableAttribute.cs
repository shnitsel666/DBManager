namespace DatabaseManager
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public readonly string TableName;

        /// <summary>
        /// Table name initialization.
        /// </summary>
        /// <param name="tableName">Table name for select, insert and update.</param>
        public TableAttribute(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                throw new DataManagerException("Table name cannot be null or empty");
            }

            TableName = tableName;
        }
    }
}
