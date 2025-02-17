using Npgsql;
using NpgsqlTypes;
using PgsqlDataFlow.Extensions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PgsqlDataFlow
{
    public class SoABulkWriter<T>
    {
        public List<Array> storage = [];
        public List<Type> types = [];
        public SoABulkWriter(string connectionString)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                //Console.WriteLine($"-------------\nType: {property.PropertyType}\n-------------");

                storage.Add(Array.CreateInstance(property.PropertyType, Constants.CHUNKSIZE));
                types.Add(property.PropertyType);
            }
        }
    }
}
