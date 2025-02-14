using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgsqlDataFlow
{
    /// <summary>
    /// Provides methods to map .NET types to PostgreSQL types.
    /// </summary>
    public static class TypeMapper
    {
        /// <summary>
        /// Maps .NET types to a collection of PostgreSQL types.
        /// </summary>
        public static readonly Dictionary<Type, HashSet<NpgsqlDbType>> EnumTypeMap = new()
        {
            [typeof(bool)] = [NpgsqlDbType.Boolean],
            [typeof(short)] = [NpgsqlDbType.Smallint],
            [typeof(ushort)] = [NpgsqlDbType.Smallint],
            [typeof(int)] = [NpgsqlDbType.Integer],
            [typeof(uint)] = [NpgsqlDbType.Integer],
            [typeof(long)] = [NpgsqlDbType.Bigint],
            [typeof(ulong)] = [NpgsqlDbType.Bigint],
            [typeof(float)] = [NpgsqlDbType.Real],
            [typeof(double)] = [NpgsqlDbType.Double],
            [typeof(decimal)] = [NpgsqlDbType.Numeric],
            [typeof(char)] = [NpgsqlDbType.Char],
            [typeof(string)] = [NpgsqlDbType.Varchar],
            [typeof(DateTime)] = [NpgsqlDbType.Timestamp, NpgsqlDbType.Date, NpgsqlDbType.TimestampTz],
            [typeof(TimeSpan)] = [NpgsqlDbType.Interval],
            [typeof(byte[])] = [NpgsqlDbType.Bytea],
            [typeof(Guid)] = [NpgsqlDbType.Uuid]
        };

        /// <summary>
        /// Gets the PostgreSQL types corresponding to the specified .NET type.
        /// </summary>
        /// <param name="type">The .NET type to map.</param>
        /// <returns>A set of PostgreSQL types corresponding to the specified .NET type.</returns>
        /// <exception cref="Exception">Thrown when the specified type is not supported.</exception>
        public static HashSet<NpgsqlDbType> GetPgsqlTypes(Type type)
        {
            if (EnumTypeMap.TryGetValue(type, out HashSet<NpgsqlDbType>? value))
                return value;
            else
                throw new Exception($"Type {type} not supported");
        }
        /// <summary>
        /// Gets the .NET type corresponding to the specified PostgreSQL type.
        /// </summary>
        /// <param name="dbType">The PostgreSQL type to map.</param>
        /// <returns>The .NET type corresponding to the specified PostgreSQL type.</returns>
        /// <exception cref="Exception">Thrown when the specified PostgreSQL type is not supported.</exception>
        public static Type GetApplicationType(NpgsqlDbType dbType)
        {
            foreach (var kvp in EnumTypeMap)
            {
                if (kvp.Value.Contains(dbType))
                    return kvp.Key;
            }

            throw new Exception($"Type {dbType} not supported");
        }
    }
}
