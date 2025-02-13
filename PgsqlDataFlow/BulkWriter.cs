using Npgsql;

namespace PgsqlDataFlow
{
    public class BulkWriter
    {
        public NpgsqlDataSource dataSource { get; set; }
        public BulkWriter(string connectionString)
        {
            dataSource = NpgsqlDataSource.Create(connectionString);
        }

    }
}
