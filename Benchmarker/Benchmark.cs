using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PgsqlDataFlow;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Reflection;

namespace Benchmarker
{
    public class Run
    {
        public static void Main()
        {
            _ = BenchmarkRunner.Run<Benchmark>();
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

        public BulkWriter<FtQueue> writer { get; set; } = new(Constants.CONNECTIONSTRING);

        public FtQueue[] entries { get; set; } = [];

        [GlobalSetup]
        [IterationSetup]
        public void InitializeEntries()
        {
            entries = new FtQueue[BatchSize];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = new FtQueue();
            }
        }

        [IterationCleanup]
        public void TruncateTable()
        {
            using var conn = writer.DataSource.OpenConnection();
            using var truncateCommand = conn.CreateCommand();
            truncateCommand.CommandText = "TRUNCATE TABLE public.ft_queue RESTART IDENTITY RESTRICT;\r\n";
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

        private IEnumerable<FtQueue> HandRolledReadCsv(string path)
        {
            List<FtQueue> result = [];
            using (var reader = new StreamReader(path))
            {
                string[] columns = (reader.ReadLine() ?? "").Replace("\"", "").Split(',');

                PropertyInfo[] properties = typeof(FtQueue).GetProperties();
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

                    Activator.CreateInstance(typeof(FtQueue), values);
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
