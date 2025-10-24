using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using TaskHub.Schema;

namespace TaskHub.Utility
{
    public static class DbUtil
    {
        private static string? _connectionString;
        public static string ConnectionString => _connectionString
            ?? throw new InvalidOperationException("Database connection has not been initialized. Call DbUtil.Initialize during application startup.");


        public static void Initialize(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("TaskHubDb")
                ?? throw new InvalidOperationException("Connection string 'TaskHubDb' is not configured.");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Unable to connect to TaskHubDb the connection string.", ex);
            }
        }


        public static async Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default)
            {
            ArgumentException.ThrowIfNullOrWhiteSpace(storedProcedureName);

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new SqlCommand(storedProcedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (parameters != null)
            {
                foreach (var (key, value) in parameters)
                {
                    var parameterName = key.StartsWith("@", StringComparison.Ordinal)
                        ? key
                        : $"@{key}";
                    command.Parameters.AddWithValue(parameterName, value ?? DBNull.Value);
                }
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var results = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var record = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false)
                        ? null
                        : reader.GetValue(i);
                    record[reader.GetName(i)] = value;
                }
                results.Add(record);
            }

            var affected = reader.RecordsAffected;
            if (results.Count == 0 && affected > 0)
            {
                results.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["RecordsAffected"] = affected
                });
            }

            return results;
        }

        public static async Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureAsync(SchemaMapping.StoredProcedures procEnum,
            IReadOnlyDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default)
            {
            if (!SchemaMapping.StoredProcedureNames.TryGetValue(procEnum, out var procName))
                throw new ArgumentException("Unknown stored procedure enum.", nameof(procEnum));

            parameters ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (SchemaMapping.StoredProcedureParameters.TryGetValue(procEnum, out var definitions))
            {
                var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var parameter in definitions)
                {
                    parameters.TryGetValue(parameter.Name, out var value);
                    normalized[parameter.Name] = ConvertParameter(value, parameter.Type);
                }
                parameters = normalized;
            }

            return await ExecuteStoredProcedureAsync(procName, parameters, cancellationToken).ConfigureAwait(false);
        }

        private static object? ConvertParameter(object? value, Type targetType)
        {
            if (value is null || targetType == typeof(string) && value is string)
                return value;

            if (targetType == typeof(DateTime))
            {
                if (value is DateTime dateTime)
                    return dateTime;
                if (DateTime.TryParse(value.ToString(), out var parsed))
                    return parsed;
                return DateTime.UtcNow;
            }

            if (targetType == typeof(int))
            {
                if (value is int i)
                    return i;
                if (int.TryParse(value?.ToString(), out var parsed))
                    return parsed;
                return 0;
            }

            if (targetType == typeof(bool))
            {
                if (value is bool b)
                    return b;
                if (bool.TryParse(value?.ToString(), out var parsed))
                    return parsed;
                return false;
            }

            if (targetType == typeof(Guid))
            {
                if (value is Guid guid)
                    return guid;
                if (Guid.TryParse(value?.ToString(), out var parsed))
                    return parsed;
                return Guid.Empty;
            }

            if (value is IConvertible)
            {
                try
                {
                    return Convert.ChangeType(value, targetType);
                }
                catch
                {
                    return value;
                }
            }

            return value;
        }

    }
}
