namespace DatabaseManager
{
    public class DBPool : IDisposable
    {
        /// <summary>
        /// Gets one connection from pool.
        /// </summary>
        public DBManager Manager => GetManager();

        private readonly DBManager[] _connections;

        private int _iterator = 0;

        private Thread _healthCheckThread;

        public DBPool(string connectionString, int connectionsCount)
        {
            if (connectionsCount <= 0)
            {
                throw new ArgumentException("Connections count must be positive value.");
            }

            if (connectionsCount >= 76)
            {
                throw new ArgumentException("Connections count must be less than 76.");
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string must not be empty.");
            }

            _connections = new DBManager[connectionsCount];

            for (int i = 0; i < connectionsCount; i++)
            {
                _connections[i] = new DBManager(connectionString);
            }

            _healthCheckThread = new Thread(() =>
            {
                HealthCheckConnections(connectionString, connectionsCount);
            })
            {
                Priority = ThreadPriority.Normal,
            };
            _healthCheckThread.Start();
        }

        private DBManager GetManager()
        {
            int index = _iterator % _connections.Length;
            DBManager manager = _connections[index];
            if (manager.Connection.State != System.Data.ConnectionState.Open)
            {
                manager = new DBManager(manager.ConnectionString);
                _connections[index] = manager;
            }

            _iterator++;
            return manager;
        }

        private bool HealthCheckConnections(string connectionString, int connectionsCount)
        {
            int i = 0;
            while (true)
            {
                try
                {
                    if (i >= connectionsCount)
                    {
                        i = 0;
                        Thread.Sleep(10000);
                        continue;
                    }

                    _connections[i].ExecuteNonQuery("PRINT 'HEALTHCHECK");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"DBPool error: connection {i} was closed.");
                    Console.WriteLine($"DBPool error: message: {e.Message}");
                    Console.WriteLine($"DBPool error: innerException: {e.InnerException}");
                    Console.WriteLine($"DBPool error: connection is closing...");
                    _connections[i].Connection.Close();
                    _connections[i].Connection.Dispose();
                    Console.WriteLine($"DBPool info: connection {i} is reopening.");
                    _connections[i] = new DBManager(connectionString);
                    Console.WriteLine($"DBPool info: connection {i} was reopened.");
                }
            }
        }

        public void Dispose()
        {
            foreach (var connection in _connections)
            {
                connection.Connection.Close();
                connection.Connection.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        ~DBPool()
        {
            Dispose();
        }
    }
}
