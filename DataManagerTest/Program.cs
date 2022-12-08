using DataManagerTest.TestModels;
using System.Linq;
using System.IO;
using Database.DataManager;

namespace DataManagerTest
{
    ///<summary>
    ///Консольное приложение для тестирования DataManager
    /// </summary>
    class Program
    {
        private static string _connectionString = "Data Source=0.0.0.0,1234; Initial Catalog=TestCatalog;Integrated Security=true;";
        static void Main(string[] args)
        {
            using(DataManager manager = new DataManager(_connectionString))
            {
                List<List<string>> str = new List<List<string>>();
                for(int i = 0; i < 10; i++)
                {
                    str.Add(new List<string>());
                }
            }
        }
    }
}
