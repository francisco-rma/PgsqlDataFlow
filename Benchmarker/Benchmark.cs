using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql;
using PgsqlDataFlow;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
                    using (var createCmd = new NpgsqlCommand($"CREATE DATABASE {targetDb}", conn))
                    {
                        createCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    Console.WriteLine($"Banco {targetDb} já existe.");
                }

                // Passo 2: Conecta no banco "testdb" e cria a tabela se não existir

                string createTableSql = @"
                CREATE TABLE IF NOT EXISTS public.test_model (
                    id_test_model BIGSERIAL PRIMARY KEY,
                    datetime_inclusion TIMESTAMP(0) NOT NULL,
                    test_measure DOUBLE PRECISION NOT NULL,
                    test_flag BOOLEAN NOT NULL,
                    name TEXT NOT NULL
                );";

                using var tableCmd = new NpgsqlCommand(createTableSql, conn);
                tableCmd.ExecuteNonQuery();
                Console.WriteLine("Tabela test_model pronta.");



            }
            using var conn2 = new NpgsqlConnection(Constants.CONNECTIONSTRING);
            conn2.Open();
            string dbPk = "";
            using (var cmd = new NpgsqlCommand(
                    "SELECT a.attname " +
                    "FROM   pg_index i " +
                    "JOIN   pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey) " +
                    "WHERE  i.indrelid = '" + " test_model " + "'::regclass AND    i.indisprimary; ",
                    conn2))
            {
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    object[] values = new object[rdr.FieldCount];
                    rdr.GetValues(values);
                    dbPk = values[0]?.ToString() ?? throw new Exception("Could not find table primary key, be sure to define one in the database");
                    Console.WriteLine( dbPk);
                }
            }
        }
    }

    [MemoryDiagnoser]
    public class Benchmark
    {
        public PooledDbContextFactory<TestDbContext> contextFactory = new(new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(
            Constants.CONNECTIONSTRING,
            opts => { opts.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(20), null); }).Options);

        [Params(100, 1000, 10000)]
        public int BatchSize { get; set; }

        public BulkWriter<TestModel> writer { get; set; } = new(Constants.CONNECTIONSTRING);

        public TestModel[] entries { get; set; } = [];

        [GlobalSetup]
        [IterationSetup]
        public void InitializeEntries()
        {
            entries = new TestModel[BatchSize];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = new TestModel();
            }
        }

        [IterationCleanup]
        public void TruncateTable()
        {
            using var conn = writer.DataSource.OpenConnection();
            using var truncateCommand = conn.CreateCommand();
            truncateCommand.CommandText = "TRUNCATE TABLE public.test_model RESTART IDENTITY RESTRICT;\r\n";
            truncateCommand.ExecuteNonQuery();
        }

        [Benchmark]
        public void BulkWrite() => writer.CreateBulk(entries);

        [Benchmark]
        public void EntityWrite()
        {
            using var context = contextFactory.CreateDbContext();
            context.AddRange(entries);
            context.SaveChanges();
        }

        private IEnumerable<TestModel> HandRolledReadCsv(string path)
        {
            List<TestModel> result = [];
            using (var reader = new StreamReader(path))
            {
                string[] columns = (reader.ReadLine() ?? "").Replace("\"", "").Split(',');

                PropertyInfo[] properties = typeof(TestModel).GetProperties();
                for (int idx = 0; idx < columns.Length; idx++)
                {
                    string col = columns[idx];
                    int j = 0;
                    while (j <= properties.Length - 1)
                    {
                        var colAnnotation = properties[j].GetCustomAttribute<ColumnAttribute>();
                        if (colAnnotation == null) { continue; }

                        if (colAnnotation.Name == col)
                        {
                            (properties[idx], properties[j]) = (properties[j], properties[idx]);
                            break;
                        }
                        else
                        {
                            j += 1;
                        }
                    }
                }

                if (columns.Length != properties.Length)
                {
                    throw new Exception("Columns count does not match properties count");
                }

                for (string? line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    string[] values = line.Split(',');

                    Activator.CreateInstance(typeof(TestModel), values);
                }
            }
            return result;
        }

        private static T[] ReadCsv<T>(string path)
        {
            using var reader = new StreamReader(path);
            var records = new CsvReader(reader, CultureInfo.InvariantCulture).GetRecords<T>();
            return records.ToArray();
        }
    }
}
