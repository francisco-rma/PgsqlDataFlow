using Npgsql;
using Npgsql.Schema;
using NpgsqlTypes;
using PgsqlDataFlow.Extensions;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;

namespace PgsqlDataFlow
{
    public class BulkWriter<T>
    {
        public NpgsqlDataSource DataSource { get; set; }
        public string DestinationTableName;

        /// <summary>
        /// Gets or sets the StringBuilder for creating SQL commands.
        /// </summary>
        public StringBuilder BuilderCreate { get; set; }

        /// <summary>
        /// Array of properties of the generic type T.
        /// Uses the same indexing as [DbColumns, ModelColumns,Types]
        /// </summary>
        public readonly PropertyInfo[] Properties = typeof(T).GetProperties();
        public int PKIdx { get; set; }
        public NpgsqlDbType PKType { get; set; }
        public NpgsqlDbType[] Types { get; set; }
        public ReadOnlyCollection<NpgsqlDbColumn> DbColumns { get; set; }
        //public NpgsqlDbColumn[] DbColumns { get; set; }
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

                if (string.IsNullOrEmpty(ModelPKName) || string.IsNullOrEmpty(DbPKName))
                    throw new Exception("No primary key found");

                DbColumns = cols;

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

                int idx = 0;
                foreach ((string colName, NpgsqlDbType colType) in dbSchema)
                {
                    ModelColumns[idx] = nameMap[colName];
                    Types[idx] = colType;
                    Properties[idx] = typeof(T).GetProperty(ModelColumns[idx]) ?? throw new Exception($"Property '{ModelColumns[idx]}' not found");

                    Type propertyType = Properties[idx].PropertyType;

                    if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        propertyType = propertyType.GetGenericArguments()[0];

                    HashSet<NpgsqlDbType> expectedTypes = TypeMapper.GetPgsqlTypes(propertyType);

                    if (!expectedTypes.Contains(colType))
                        throw new Exception($"Property '{Properties[idx].Name}' does not match it's column data type counterpart.\n" +
                                            $"Found type {Properties[idx].PropertyType}, expected {TypeMapper.GetApplicationType(Types[idx]).Name}");

                    if ((DbColumns[idx].IsAutoIncrement ?? false)) { }
                    else if (BuilderCreate == null) { BuilderCreate = new(colName); }
                    else { BuilderCreate.Append(", " + colName); }

                    idx += 1;
                }
            }

            if (BuilderCreate == null) throw new Exception("No columns found");
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
        private static Dictionary<string, string> DbToModel(Span<PropertyInfo> properties)
        {
            Dictionary<string, string> dict = [];
            foreach (PropertyInfo prop in properties)
            {
                var col = prop.GetCustomAttribute<ColumnAttribute>();
                if (col == null) { continue; }
                if (!string.IsNullOrEmpty(col.Name))
                    dict.Add(col.Name, prop.Name);

            }
            return dict;
        }

        /// <summary>
        /// Creates and sends a batch of rows to the database using the binary COPY protocol.
        /// Ensures that the data types of model properties exactly match their corresponding database columns.
        /// </summary>
        /// <param name="sourceList">The span of models to be inserted into the database.</param>
        /// <exception cref="InvalidCastException">
        /// Thrown if a property type does not match the corresponding database column type (e.g., using a <c>long</c> (int64) for an <c>int4</c> column).
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the database connection is lost during the operation.
        /// </exception>
        public void CreateAutoIncrementBulk(Span<T> sourceList)
        {
            using var conn = DataSource.OpenConnection();

            using NpgsqlBinaryImporter writer = conn.BeginBinaryImport("COPY " + DestinationTableName + " (" + BuilderCreate.ToString() + ") FROM STDIN (FORMAT BINARY)");
            ConstructTable(writer, sourceList);
            writer.Complete();
        }

        /// <summary>
        /// Creates and sends a batch of rows to the database using the binary COPY protocol.
        /// Ensures that the data types of model properties exactly match their corresponding database columns.
        /// </summary>
        /// <param name="sourceList">The list of models to be inserted into the database.</param>
        /// <exception cref="InvalidCastException">
        /// Thrown if a property type does not match the corresponding database column type (e.g., using a <c>long</c> (int64) for an <c>int4</c> column).
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the database connection is lost during the operation.
        /// </exception>

        public void CreateAutoIncrementBulk(List<T> sourceList)
        {
            using var conn = DataSource.OpenConnection();

            using NpgsqlBinaryImporter writer = conn.BeginBinaryImport("COPY " + DestinationTableName + " (" + BuilderCreate.ToString() + ") FROM STDIN (FORMAT BINARY)");
            ConstructTable(writer, sourceList);
            writer.Complete();
        }
        private void ConstructTable(NpgsqlBinaryImporter writer, Span<T> sourceList)
        {
            for (int i = 0; i < sourceList.Length; i++)
            {
                var item = sourceList[i];

                writer.StartRow();

                for (int j = 0; j < DbColumns.Count; j++)
                {
                    if (ModelColumns[j] == ModelPKName) { continue; }

                    object? value = Properties[j].GetValue(item);

                    if (value == null) { writer.WriteNull(); }

                    else
                    {
                        switch (Types[j])
                        {
                            case NpgsqlDbType.Bigint:
                                writer.Write((long)value, Types[j]);
                                break;

                            case NpgsqlDbType.Boolean:
                                writer.Write((bool)value, Types[j]);
                                break;

                            case NpgsqlDbType.Integer:
                                writer.Write((int)value, Types[j]);
                                break;

                            case NpgsqlDbType.Smallint:
                                writer.Write((short)value, Types[j]);
                                break;

                            case NpgsqlDbType.Numeric:
                                writer.Write((decimal)value, Types[j]);
                                break;

                            case NpgsqlDbType.Timestamp:
                            case NpgsqlDbType.Date:
                                writer.Write(((DateTime)value).SetKind(DateTimeKind.Unspecified), Types[j]);
                                break;

                            case NpgsqlDbType.TimestampTz:
                                writer.Write(((DateTime)value).SetKind(DateTimeKind.Utc), Types[j]);
                                break;

                            case NpgsqlDbType.Varchar:
                                writer.Write((string)value, Types[j]);
                                break;
                        }
                    }
                }
            }
        }
        private void ConstructTable(NpgsqlBinaryImporter writer, List<T> sourceList)
        {
            for (int i = 0; i < sourceList.Count; i++)
            {
                var item = sourceList[i];

                writer.StartRow();

                for (int j = 0; j < DbColumns.Count; j++)
                {
                    if (ModelColumns[j] == ModelPKName) { continue; }

                    object? value = Properties[j].GetValue(item);

                    if (value == null) { writer.WriteNull(); }

                    else
                    {
                        switch (Types[j])
                        {
                            case NpgsqlDbType.Bigint:
                                writer.Write((long)value, Types[j]);
                                break;

                            case NpgsqlDbType.Boolean:
                                writer.Write((bool)value, Types[j]);
                                break;

                            case NpgsqlDbType.Integer:
                                writer.Write((int)value, Types[j]);
                                break;

                            case NpgsqlDbType.Smallint:
                                writer.Write((short)value, Types[j]);
                                break;

                            case NpgsqlDbType.Numeric:
                                writer.Write((decimal)value, Types[j]);
                                break;

                            case NpgsqlDbType.Timestamp:
                            case NpgsqlDbType.Date:
                                writer.Write(((DateTime)value).SetKind(DateTimeKind.Unspecified), Types[j]);
                                break;

                            case NpgsqlDbType.TimestampTz:
                                writer.Write(((DateTime)value).SetKind(DateTimeKind.Utc), Types[j]);
                                break;

                            case NpgsqlDbType.Varchar:
                                writer.Write((string)value, Types[j]);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the index of the specified property name in the model columns.
        /// </summary>
        /// <param name="propertyName">The name of the property to find the index for.</param>
        /// <returns>The index of the specified property name in the model columns.</returns>
        public int GetColumnIndex(string propertyName)
        {
            int idx = Array.IndexOf(ModelColumns, propertyName);
            return idx;
        }
    }
}
