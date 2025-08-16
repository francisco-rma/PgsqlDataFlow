using Npgsql;
using Npgsql.Schema;
using NpgsqlTypes;
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
        public int PKIdx { get; set; }
        public NpgsqlDbType PKType { get; set; }
        public Tuple<string, NpgsqlDbType, NpgsqlDbColumn>[] TypeModelBindings { get; set; }
        public string DbPKName { get; set; }
        public string ModelPKName { get; set; }
        public BulkWriter(string connectionString)
        {
            DestinationTableName = GetModelTableName();
            DataSource = NpgsqlDataSource.Create(connectionString);

            using var conn = DataSource.OpenConnection();

            ModelPKName = GetModelPrimaryKey();
            DbPKName = GetDbPrimaryKey(conn, DestinationTableName);

            Dictionary<string, NpgsqlDbType> dbSchema = [];

            using (var cmd = new NpgsqlCommand("SELECT * FROM " + DestinationTableName + " LIMIT 1", conn))
            {
                using var rdr = cmd.ExecuteReader(System.Data.CommandBehavior.KeyInfo);
                var schema = rdr.GetSchemaTable() ?? throw new Exception($"Could not find table schema for table {DestinationTableName}");
                var DbColumns = rdr.GetColumnSchema();


                if (string.IsNullOrEmpty(ModelPKName) || string.IsNullOrEmpty(DbPKName))
                    throw new Exception("No primary key found");

                for (int i = 0; i < DbColumns.Count; i++)
                {
                    string name = DbColumns[i].ColumnName;
                    NpgsqlDbType type = DbColumns[i].NpgsqlDbType ?? NpgsqlDbType.Unknown;

                    dbSchema.Add(name, type);
                    if (name == DbPKName)
                    {
                        PKType = type;
                        PKIdx = i;
                    }
                }

                Dictionary<string, string> nameMap = GenerateNameMap();

                if (nameMap.Count != dbSchema.Count) throw new Exception("Model and database schema do not match");

                TypeModelBindings = new Tuple<string, NpgsqlDbType, NpgsqlDbColumn>[dbSchema.Count];

                var tempProperties = new PropertyInfo[dbSchema.Count];

                int idx = 0;

                //TODO figure out if there's a way to annotate Postgresql's expected data type in the model as well
                foreach ((string colName, NpgsqlDbType colType) in dbSchema)
                {
                    TypeModelBindings[idx] = new Tuple<string, NpgsqlDbType, NpgsqlDbColumn>(
                        nameMap[colName],
                        colType,
                        DbColumns.First(col => col.ColumnName == colName));

                    PropertyInfo property = typeof(T).GetProperty(TypeModelBindings[idx].Item1)
                        ?? throw new Exception($"Property '{TypeModelBindings[idx].Item1}' not found");

                    tempProperties[idx] = property;

                    Type propertyType = property.PropertyType;

                    if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        propertyType = propertyType.GetGenericArguments()[0];

                    if (!TypeMapper.GetPgsqlTypes(propertyType).Contains(colType))
                        throw new Exception($"Property '{property.Name}' does not match it's column data type counterpart.\n" +
                                            $"Found type {property.PropertyType}, expected {TypeMapper.GetApplicationType(TypeModelBindings[idx].Item2).Name}");

                    if (!(DbColumns[idx].IsAutoIncrement ?? DbColumns[idx].ColumnName == DbPKName))
                    {
                        if (BuilderCreate == null) { BuilderCreate = new(colName); }
                        else { BuilderCreate.Append(", " + colName); }
                    }

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

        /// <summary>
        /// Gets the index of the specified property name in the model columns.
        /// </summary>
        /// <param name="propertyName">The name of the property to find the index for.</param>
        /// <returns>The index of the specified property name in the model columns.</returns>
        public int GetColumnIndex(string propertyName)
        {
            int idx = 0;
            foreach ((string name, NpgsqlDbType type, NpgsqlDbColumn _) in TypeModelBindings)
            {
                if (name == propertyName)
                    return idx;
                idx += 1;
            }
            throw new Exception("Column not found");
        }

        public string GetModelPrimaryKey()
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                if (prop.GetCustomAttribute<KeyAttribute>() is KeyAttribute _)
                    return prop.Name;
            }

            throw new Exception("Could not find table primary key, be sure to annotate it on the model file");
        }

        /// <summary>
        /// Retrieves the primary key column name from the database.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetDbPrimaryKey(NpgsqlConnection connection, string tableName)
        {
            string dbPk = "";
            using (var cmd = new NpgsqlCommand(
                    "SELECT a.attname " +
                    "FROM   pg_index i " +
                    "JOIN   pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey) " +
                    "WHERE  i.indrelid = '" + tableName + "'::regclass AND    i.indisprimary; ",
                    connection))
            {
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    object[] values = new object[rdr.FieldCount];
                    rdr.GetValues(values);
                    dbPk = values[0]?.ToString() ?? throw new Exception("Could not find table primary key, be sure to define one in the database");
                }
            }

            return dbPk;
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
        public void CreateBulk(Span<T> sourceList)
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

                for (int j = 0; j < TypeModelBindings.Length; j++)
                {
                    object? value = PropertyAccessors<T>.Getters[TypeModelBindings[j].Item1](item);

                    if (TypeModelBindings[j].Item3.IsAutoIncrement.HasValue && TypeModelBindings[j].Item3.IsAutoIncrement.Value)
                    {
                        if (value is not null && !IsDefault(BulkWriter<T>.TypeSwitch(TypeModelBindings[j].Item2, value)))
                            throw new Exception($"Auto increment column\n" +
                                $"({TypeModelBindings[j].Item3.DataTypeName}){TypeModelBindings[j].Item3.ColumnName}:{TypeModelBindings[j].Item1}" +
                                $"\nshould be null");

                        continue;
                    }

                    if (value is null)
                    {
                        writer.WriteNull();
                    }
                    else
                    {
                        writer.Write(BulkWriter<T>.TypeSwitch(TypeModelBindings[j].Item2, value));
                    }
                }
            }
        }

        public async void CreateBulkAsync(Memory<T> sourceMemory)
        {
            using var conn = DataSource.OpenConnection();
            using NpgsqlBinaryImporter writer = conn.BeginBinaryImport("COPY " + DestinationTableName + " (" + BuilderCreate.ToString() + ") FROM STDIN (FORMAT BINARY)");
            ConstructTableAsync(writer, sourceMemory);
            await writer.CompleteAsync();
        }

        private async void ConstructTableAsync(NpgsqlBinaryImporter writer, Memory<T> sourceMemory)
        {
            for (int i = 0; i < sourceMemory.Span.Length; i++)
            {
                var item = sourceMemory.Span[i];

                await writer.StartRowAsync();

                for (int j = 0; j < TypeModelBindings.Length; j++)
                {
                    object? value = PropertyAccessors<T>.Getters[TypeModelBindings[j].Item1](item);

                    if (TypeModelBindings[j].Item3.IsAutoIncrement.HasValue && TypeModelBindings[j].Item3.IsAutoIncrement.Value)
                    {
                        if (value is not null && !IsDefault(BulkWriter<T>.TypeSwitch(TypeModelBindings[j].Item2, value)))
                            throw new Exception($"Auto increment column\n" +
                                $"({TypeModelBindings[j].Item3.DataTypeName}){TypeModelBindings[j].Item3.ColumnName}:{TypeModelBindings[j].Item1}" +
                                $"\nshould be null");

                        continue;
                    }

                    if (value is null)
                    {
                        await writer.WriteNullAsync();
                    }
                    else
                    {
                        await writer.WriteAsync(BulkWriter<T>.TypeSwitch(TypeModelBindings[j].Item2, value));
                    }
                }
            }
        }

        /// <summary>
        /// Updates a specific column in the database for a batch of rows using the binary COPY protocol.
        /// Ensures that the data types of model properties exactly match their corresponding database columns.
        /// </summary>
        /// <param name="sourceList">The list of models to be updated in the database.</param>
        /// <param name="colIndex">The index of the column to be updated.</param>
        /// <exception cref="InvalidCastException">
        /// Thrown if a property type does not match the corresponding database column type (e.g., using a <c>long</c> (int64) for an <c>int4</c> column).
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the database connection is lost during the operation.
        /// </exception>
        public void UpdateColumnBulk(Span<T> sourceList, int colIndex)
        {
            using var conn = DataSource.OpenConnection();

            string tempName = "update_temp_table";

            string colName = TypeModelBindings[colIndex].Item3.ColumnName;
            string colTypeName = TypeModelBindings[colIndex].Item1.ToString().ToLower();

            using (var createTempTable = conn.CreateCommand())
            {
                createTempTable.CommandText = "CREATE TEMP TABLE " + tempName + " ("
                    + (DbPKName + "_temp " + PKType.ToString().ToLower())
                    + ", "
                    + (colName) + "_temp " + TypeModelBindings[colIndex].Item1.ToString().ToLower() + ")";
                createTempTable.ExecuteNonQuery();
            }
            ;

            using (var writer = conn.BeginBinaryImport("COPY " + tempName + " ("
                + DbPKName + "_temp "
                + ", "
                + (colName) + "_temp " + ") "
                + "FROM STDIN (FORMAT BINARY)"))
            {
                foreach (var item in sourceList)
                {
                    writer.StartRow();

                    object? colValue = PropertyAccessors<T>.Getters[TypeModelBindings[colIndex].Item1](item);
                    object? pkValue = PropertyAccessors<T>.Getters[TypeModelBindings[PKIdx].Item1](item);

                    if (pkValue is null)
                    {
                        writer.WriteNull();
                    }
                    else
                    {
                        writer.Write(BulkWriter<T>.TypeSwitch(TypeModelBindings[colIndex].Item2, pkValue));
                    }

                    if (colValue is null)
                    {
                        writer.WriteNull();
                    }
                    else
                    {
                        writer.Write(BulkWriter<T>.TypeSwitch(TypeModelBindings[colIndex].Item2, colValue));
                    }
                }
                writer.Complete();
            }

            using var update = conn.CreateCommand();
            update.CommandText = "UPDATE " + DestinationTableName
                + " SET " + TypeModelBindings[colIndex].Item3.ColumnName + " = " + tempName + "." + TypeModelBindings[colIndex].Item3.ColumnName + "_temp"
                + " FROM " + tempName + " WHERE " + DestinationTableName + "." + DbPKName + " = " + tempName + "." + DbPKName + "_temp";

            Console.WriteLine(update.CommandText);

            update.ExecuteNonQuery();
        }
        public void SimulateBulk(Span<T> source)
        {
            using var conn = DataSource.OpenConnection();
            NpgsqlBinaryImporter writer = conn.BeginBinaryImport("COPY " + DestinationTableName + " (" + BuilderCreate.ToString() + ") FROM STDIN (FORMAT BINARY)");
            ConstructTable(writer, source);
            writer.Dispose();
            return;
        }
        private static Dictionary<string, string> GenerateNameMap()
        {
            Dictionary<string, string> nameMap = [];
            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                var col = prop.GetCustomAttribute<ColumnAttribute>();
                if (col is not null && !string.IsNullOrEmpty(col.Name))
                {
                    nameMap[col.Name] = prop.Name;
                }
            }

            if (nameMap.Count < 1) throw new Exception("Table must have at least 1 column");
            return nameMap;
        }
        private static dynamic TypeSwitch(NpgsqlDbType type, object value)
        {
            switch (type)
            {
                case NpgsqlDbType.Bigint:
                    return (long)value;

                case NpgsqlDbType.Boolean:
                    return (bool)value;

                case NpgsqlDbType.Integer:
                    return (int)value;

                case NpgsqlDbType.Smallint:
                    return (short)value;

                case NpgsqlDbType.Numeric:
                    return (decimal)value;

                case NpgsqlDbType.Timestamp:
                case NpgsqlDbType.Date:
                    return DateTime.SpecifyKind((DateTime)value, DateTimeKind.Unspecified);

                case NpgsqlDbType.TimestampTz:
                    return DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);

                case NpgsqlDbType.Varchar:
                    return (string)value;

                default:
                    return value;
            }
        }
        public bool IsDefault<D>(D value)
        {
            bool result = EqualityComparer<D>.Default.Equals(value, default(D));
            return result;
        }
    }
}
