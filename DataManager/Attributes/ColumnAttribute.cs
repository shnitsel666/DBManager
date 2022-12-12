namespace DatabaseManager
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ColumnAttribute: Attribute
    {
        public readonly string ColumnName;
        ///<summary>
        ///Column name is being chosen by default (field name in class)
        /// </summary>
        public ColumnAttribute() =>
            ColumnName = string.Empty;

        ///<summary>
        ///Set column name in database
        /// </summary>
        public ColumnAttribute(string columnName)
        {
            if(string.IsNullOrEmpty(columnName))
            {
                throw new DataManagerException("Column name cannot be empty or null");
            }
            ColumnName = columnName;
        }
    }
}
