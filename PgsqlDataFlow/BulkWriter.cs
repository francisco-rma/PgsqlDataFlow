using Npgsql;
using Npgsql.Schema;
using NpgsqlTypes;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace PgsqlDataFlow
{
    public class BulkWriter<T>
    {
        public NpgsqlDataSource DataSource { get; set; }
        public string DestinationTableName;

        /// <summary>
        /// Array of properties of the generic type T.
        /// Uses the same indexing as [DbColumns, ModelColumns,Types]
        /// </summary>
        public readonly PropertyInfo[] Properties = typeof(T).GetProperties();
        public int PKIdx { get; set; }
        public NpgsqlDbType PKType { get; set; }
        public NpgsqlDbType[] Types { get; set; }
        //public ReadOnlyCollection<NpgsqlDbColumn> DbColumns { get; set; }
        public string[] DbColumns { get; set; }
        public string DbPKName { get; set; }
        public string[] ModelColumns { get; set; }
        public string ModelPKName { get; set; }
        public BulkWriter(string connectionString)
        {
            DestinationTableName = GetModelTableName();

            DataSource = NpgsqlDataSource.Create(connectionString);
            ModelPKName = GetModelPrimaryKey();



            Dictionary<string, NpgsqlDbType> dbSchema = [];

            using var conn = DataSource.CreateConnection();
            using (var cmd = new NpgsqlCommand("SELECT * FROM " + DestinationTableName + " LIMIT 1", conn))
            {
                using var rdr = cmd.ExecuteReader();
                var schema = rdr.GetSchemaTable() ?? throw new Exception($"Could not find table schema for table {DestinationTableName}");
                var cols = rdr.GetColumnSchema();

                DbPKName = schema.PrimaryKey[0].ColumnName;

                for (int i = 0; i < cols.Count; i++)
                {
                    string name = cols[i].ColumnName;
                    NpgsqlDbType type = cols[i].NpgsqlDbType ?? NpgsqlDbType.Unknown;

                    dbSchema.Add(name, type);
                    if (name == DbPKName)
                    {
                        PKType = type;
                        PKIdx = i;
                    }
                }

                var nameMap = DbToModel(Properties);

                if (nameMap.Count < 1) throw new Exception("Table must have at least 1 column");
                if (nameMap.Count != dbSchema.Count) throw new Exception("Model and database schema do not match");

                Types = new NpgsqlDbType[dbSchema.Count];
                ModelColumns = new string[dbSchema.Count];
                DbColumns = new string[dbSchema.Count];

                int idx = 0;
                foreach ((string colName, NpgsqlDbType colType) in dbSchema)
                {
                    if (colName == DbPKName) { PKType = colType; PKIdx = idx; }
                    DbColumns[idx] = colName;
                    ModelColumns[idx] = nameMap[colName];
                    Types[idx] = colType;
                }
            }

            if (string.IsNullOrEmpty(ModelPKName) || string.IsNullOrEmpty(DbPKName))
                throw new Exception("No primary key found");
        }

        /// <summary>
        /// Retrieves the database table name from the model class.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetModelTableName()
        {
            if (typeof(T).GetCustomAttribute<TableAttribute>() is TableAttribute table)
                return table.Name;

            throw new Exception("Could not find table name, be sure to annotate it on the model file");
        }

        public string GetModelPrimaryKey()
        {
            foreach (var prop in Properties)
            {
                if (prop.GetCustomAttribute<KeyAttribute>() is KeyAttribute _)
                    return prop.Name;
            }

            throw new Exception("Could not find table primary key, be sure to annotate it on the model file");
        }
        private Dictionary<string, string> DbToModel(PropertyInfo[] properties)
        {
            Dictionary<string, string> dict = [];
            foreach (PropertyInfo prop in properties)
            {
                {
                    var col = prop.GetCustomAttribute<ColumnAttribute>();
                    if (col == null) { continue; }
                    if (!string.IsNullOrEmpty(col.Name))
                        dict.Add(col.Name, prop.Name);
                }
            }
            return dict;
        }
    }
}
