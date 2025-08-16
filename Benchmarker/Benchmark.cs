using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using PgsqlDataFlow;

namespace Benchmarker
{
    public class Run
    {
        public static void Main()
        {
            TestPostgresConnection();
            _ = BenchmarkRunner.Run<Benchmark>();
        }
        public static void TestPostgresConnection()
        {
            string targetDb = "testdb";

            using (NpgsqlConnection conn = new($"Host={Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost"};" +
            $"Port={Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432"};" +
            $"Pooling=true;" +
            $"Database=postgres;" +
            $"User Id=postgres;" +
            $"Password=postgres;" +
            $""))
            {
                conn.Open();

                using var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = @dbname", conn);
                cmd.Parameters.AddWithValue("dbname", targetDb);
                var exists = cmd.ExecuteScalar();

                if (exists == null)
                {
                    Console.WriteLine($"Banco {targetDb} não existe. Criando...");
                    using var createCmd = new NpgsqlCommand($"CREATE DATABASE {targetDb}", conn);
                    createCmd.ExecuteNonQuery();
                }
                else
                {
                    Console.WriteLine($"Banco {targetDb} já existe.");
                }
            }

            // Passo 2: Conecta no banco "testdb" e cria a tabela se não existir
            using var conn2 = new NpgsqlConnection(Constants.CONNECTIONSTRING);
            conn2.Open();

            string createTableSql = "CREATE TABLE IF NOT EXISTS test_model (id_test_model BIGSERIAL PRIMARY KEY,datetime_inclusion TIMESTAMP(0) NOT NULL,test_measure DOUBLE PRECISION NOT NULL,test_flag BOOLEAN NOT NULL,name TEXT NOT NULL);";
            using var tableCmd = new NpgsqlCommand(createTableSql, conn2);
            Console.WriteLine(tableCmd.CommandText);
            tableCmd.ExecuteNonQuery();
            Console.WriteLine("Tabela test_model pronta.");
        }
    }

    [MemoryDiagnoser]
    public class Benchmark
    {
        public PooledDbContextFactory<TestDbContext> contextFactory = new(new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(
            Constants.CONNECTIONSTRING,
            opts => { opts.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(20), null); }).Options);

        [Params(50, 5000, 50000)]
        public int BatchSize { get; set; }

        public BulkWriter<TestModel> Writer { get; set; } = new(Constants.CONNECTIONSTRING);

        public TestModel[] Entries { get; set; } = [];

        [GlobalSetup]
        [IterationSetup]
        public void InitializeEntries()
        {
            Entries = new TestModel[BatchSize];
            for (int i = 0; i < Entries.Length; i++)
            {
                Entries[i] = new TestModel();
            }
        }

        [IterationCleanup]
        public void TruncateTable()
        {
            using var conn = Writer.DataSource.OpenConnection();
            using var truncateCommand = conn.CreateCommand();
            truncateCommand.CommandText = "TRUNCATE TABLE public.test_model RESTART IDENTITY RESTRICT;\r\n";
            truncateCommand.ExecuteNonQuery();
        }

        [Benchmark]
        public void BulkWrite() => Writer.CreateBulk(Entries);

        [Benchmark]
        public void EntityWrite()
        {
            using var context = contextFactory.CreateDbContext();
            context.AddRange(Entries);
            context.SaveChanges();
        }
    }
}
