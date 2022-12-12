namespace DataManagerTest
{
    using DatabaseManager;

    /// <summary>
    /// Console Application for DBManager testing.
    /// </summary>
    class Program
    {
        public static void Main(string[] args)
        {
            DBManagerPool _DBManagerPool = new();
            _DBManagerPool.DefaultManager.ExecuteNonQuery("SELECT 1 = 1");
        }
    }

    public class DBManagerPool
    {
        private readonly string _defaultConnectionString = "Data Source=0.0.0.0,1234; Initial Catalog=TestCatalog;Integrated Security=true;";

        private readonly string _1CConnectionString = "Data Source=0.0.0.0,1234; Initial Catalog=TestCatalog;Integrated Security=true;";

        private readonly DBPool dbPool1;

        private readonly DBPool dbPool2;

        public DBManager DefaultManager => dbPool1.Manager;

        public DBManager _1CManager => dbPool2.Manager;

        public DBManagerPool()
        {
            dbPool1 = new DBPool(_defaultConnectionString, 10);
            dbPool2 = new DBPool(_1CConnectionString, 10);
        }
    }
}
