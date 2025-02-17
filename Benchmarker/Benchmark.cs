using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using CommandLine;
using CsvHelper;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NpgsqlTypes;
using PgsqlDataFlow;
using System;
using System.Buffers;
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
            var result = BenchmarkRunner.Run<Benchmark>();
            Console.WriteLine(result.AllRuntimes);
            int i = 0;
            foreach (var report in result.Reports)
            {
                Console.WriteLine($"\n-------------Report {i}-------------");
                Console.WriteLine(report.ToString());
                Console.WriteLine($"------------------------------------\n");
                i += 1;
            }

            //Benchmark benchmark = new();
            //benchmark.SoAConstruction();
            //benchmark.ModelAllocation();
        }
    }



    [MemoryDiagnoser]
    public class Benchmark
    {
        //[Params(10, 100, 1000)]
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

        //[GlobalSetup]
        public void Setup()
        {
            SampleSpan = ReadCsv<FtQueue>("C:\\Users\\User\\projetos\\PgsqlDataFlow\\Benchmarker\\ft_queue_sample.csv")[0..size];
            SampleList = ReadCsv<FtQueue>("C:\\Users\\User\\projetos\\PgsqlDataFlow\\Benchmarker\\ft_queue_sample.csv").ToList()[0..size];
        }

        //[Benchmark]
        public void SpanConstruction()
        {
            writer.SimulateBulk(SampleSpan.AsSpan(0, size));
        }

        //[Benchmark]
        public void SpanConstructionCustom()
        {
            writerCustom.SimulateBulk(SampleSpan.AsSpan(0, size));
        }

        [Benchmark]
        public void SoAConstruction()
        {
            Span<PropertyInfo> properties = typeof(FtQueue).GetProperties();
            Span<Type> types = new Type[properties.Length];
            Span<Array> storage = new Array[properties.Length];
            int idx = 0;
            foreach (PropertyInfo property in properties)
            {
                //storage[idx] = Array.CreateInstance(property.PropertyType, Constants.CHUNKSIZE);
                storage[idx] = ArrayPool<int>.Shared.Rent(Constants.CHUNKSIZE);
                types[idx] = property.PropertyType;
                idx += 1;
            }
            HandRolledReadCsv(Constants.PATH, storage, types);
        }

        [Benchmark]
        public void ModelAllocation()
        {
            PropertyInfo[] properties = typeof(FtQueue).GetProperties();
            Array[] storage = new Array[properties.Length];
            Type[] types = new Type[properties.Length];
            int idx = 0;
            foreach (PropertyInfo property in properties)
            {
                storage[idx] = Array.CreateInstance(property.PropertyType, Constants.CHUNKSIZE);
                types[idx] = property.PropertyType;
                idx += 1;
            }
            List<FtQueue> test = [.. ReadCsv<FtQueue>(Constants.PATH)];
        }

        private void HandRolledReadCsv(string path, Span<Array> destination, Span<Type> types)
        {
            using (var reader = new StreamReader(path))
            {
                string[] columns = (reader.ReadLine() ?? "").Replace("\"", "").Split(',');

                int i = 0;
                for (var data = reader.ReadLine(); data != null;)
                {
                    string[] values = data.Replace("\"", "").Split(',');
                    for (int j = 0; j < columns.Length; j++)
                    {
                        var type = destination[j].GetValue(0)?.GetType();
                        if (type is not null)
                        {
                            destination[j].SetValue(Convert.ChangeType(values[j], type), i);
                        }
                    }
                    i += 1;
                    data = reader.ReadLine();
                }
            }
            return;
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
