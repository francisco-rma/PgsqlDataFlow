using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CsvHelper;
using NpgsqlTypes;
using PgsqlDataFlow;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Globalization;
using System.Reflection;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Benchmarker
{
    public class Program
    {
        public static void Main()
        {
            _ = BenchmarkRunner.Run<Benchmark>();
            //var writer = new BulkWriter<FtQueue>(Constants.CONNECTIONSTRING);
        }
    }



    [MemoryDiagnoser]
    public class Benchmark
    {
        [Params(10, 100, 1000)]
        public int size { get; set; }
        public BulkWriter<FtQueue> writer { get; set; } = new(Constants.CONNECTIONSTRING);
        public BulkWriter<FtQueue> writerCustom { get; set; } = new(Constants.CUSTOMCONNECTIONSTRING);
        public FtQueue[] SampleSpan { get; set; } = [.. ReadCsv<FtQueue>("C:\\Users\\User\\projetos\\PgsqlDataFlow\\Benchmarker\\ft_queue_sample.csv")];
        public List<FtQueue> SampleList { get; set; } = [.. ReadCsv<FtQueue>("C:\\Users\\User\\projetos\\PgsqlDataFlow\\Benchmarker\\ft_queue_sample.csv")];
        private static T[] ReadCsv<T>(string path)
        {
            using var reader = new StreamReader(path);
            var records = new CsvReader(reader, CultureInfo.InvariantCulture).GetRecords<T>();
            return records.ToArray();
        }

        [GlobalSetup]
        public void Setup()
        {
            SampleSpan = ReadCsv<FtQueue>("C:\\Users\\User\\projetos\\PgsqlDataFlow\\Benchmarker\\ft_queue_sample.csv")[0..size];
            SampleList = ReadCsv<FtQueue>("C:\\Users\\User\\projetos\\PgsqlDataFlow\\Benchmarker\\ft_queue_sample.csv").ToList()[0..size];
        }

        //[Benchmark]
        //public void ListConstruction()
        //{
        //    writer.SimulateBulk(SampleList);
        //}
        [Benchmark]
        public void SpanConstruction()
        {
            writer.SimulateBulk(SampleSpan.AsSpan(0, size));
        }

        [Benchmark]
        public void SpanConstructionCustom()
        {
            writerCustom.SimulateBulk(SampleSpan.AsSpan(0, size));
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
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
