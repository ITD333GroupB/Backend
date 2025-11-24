using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using TaskHub.Schema;
using TaskHub.Schema.WorkingItems;

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

        private static async Task<Tasks> GetTaskWithTaskId(int taskId)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand("");
            command.Parameters.AddWithValue("TaskId", taskId);
            using(SqlDataReader datareader = command.ExecuteReader())
            {
                if (datareader.HasRows)
                {
                    Tasks task = new Tasks();
                    while (datareader.Read())
                    {
                        // Map datareader columns to task properties
                        task.TaskId = datareader.GetInt32(0);
                        task.ParentGroupId = datareader.GetInt32(1);
                        task.ParentWorkspaceId = datareader.GetInt32(2);
                        task.Status = (Schema.WorkingItems.TaskStatus)datareader.GetInt32(4);
                        task.Description = datareader.GetString(5);
                        task.Title = datareader.GetString(6);
                    }
                    return task;
                }
            }
            return null;
        }

        public static async Task<List<Tasks>> GetListTaskWithUserId(int userId)
        {
            List<int> taskIds = new List<int>();

            List<Tasks> list = new List<Tasks>();

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            await using var command = new SqlCommand(StoredProcedureNames[StoredProcedures.GetUserTasks], connection)
            {
                CommandType = CommandType.StoredProcedure
            }; // set command to name of stored procedure, mark as stored procedure prior to executing

            // pass user Id as param to SQL command
            command.Parameters.AddWithValue("UserId", userId);

            using(SqlDataReader datareader = command.ExecuteReader())
            {
                if (datareader.HasRows)
                {
                    while(datareader.Read())
                    {
                        taskIds.Add(datareader.GetInt32(0)); // assuming TaskID is the first column
                    }
                }
            }

            if(taskIds.Count > 0)
            {
                // Run sequential logic to get the actual individual task via the TaskId
                foreach(int id in taskIds)
                {
                    Tasks task = await GetTaskWithTaskId(id);
                    if (task != null)
                    {
                        list.Add(task);
                    }
                }
            }

            return list;
        }

        // TODO Remove this method post-refactor
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

        // TODO Remove this method post-refactor
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
        // TODO probably need to remove this method post-refactor
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

        /// <summary>
        /// Retrieves a list of tasks associated with the specified workspace identifier.
        /// </summary>
        /// <param name="workspaceId">The ID of the workspace for which to retrieve tasks.</param>
        /// <returns>The task result contains a list of tasks belonging to the
        /// specified workspace. The list is empty if no tasks are found.</returns>
        public static async Task<List<Tasks>> GetTasksByWorkspaceId(int workspaceId)
        {
            var list = new List<Tasks>();
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand(SchemaMapping.StoredProcedureNames[SchemaMapping.StoredProcedures.GetWorkspaceTasks], connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("workspaceID", workspaceId);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var task = new Tasks
                {
                    TaskId = GetInt(reader, "TaskID"),
                    ParentGroupId = GetInt(reader, "GroupID"),
                    ParentWorkspaceId = GetInt(reader, "WorkspaceID"),
                    Status = (Schema.WorkingItems.TaskStatus)GetInt(reader, "TaskStatus"),
                    Description = GetString(reader, "TaskContents"),
                    Title = GetString(reader, "TaskName")
                };
                list.Add(task);
            }
            return list;
        }

        /// <summary>
        /// Retrieves all workspaces that belong to the specified group identifier.
        /// </summary>
        /// <param name="groupId">The ID of the group whose workspaces are being requested.</param>
        /// <returns>Returns a list of workspace objects tied to the group. The list is empty if none are found.</returns>
        public static async Task<List<Workspace>> GetWorkspacesByGroupId(int groupId)
        {
            var list = new List<Workspace>();
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand(SchemaMapping.StoredProcedureNames[SchemaMapping.StoredProcedures.GetGroupWorkspaces], connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("groupID", groupId);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var ws = new Workspace
                {
                    WorkspaceId = GetInt(reader, "WorkspaceID"),
                    ParentGroupId = GetInt(reader, "GroupID"),
                    Name = GetString(reader, "Name"),
                    Description = GetString(reader, "Description")
                };
                list.Add(ws);
            }
            return list;
        }

        /// <summary>
        /// Pulls the list of groups associated with the given user identifier.
        /// </summary>
        /// <param name="userId">The ID of the user for whom group membership/ownership is being fetched.</param>
        /// <returns>Returns a list of group records for the user. Empty list if user is not in any groups.</returns>
        public static async Task<List<Group>> GetGroupsByUserId(int userId)
        {
            var list = new List<Group>();
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand(SchemaMapping.StoredProcedureNames[SchemaMapping.StoredProcedures.GetUserGroups], connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("userID", userId);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var g = new Group
                {
                    GroupId = GetInt(reader, "ID"),
                    Name = GetString(reader, "Name"),
                    Description = GetString(reader, "Description"),
                    OwnerId = GetInt(reader, "OwnerID")
                };
                list.Add(g);
            }
            return list;
        }

        /// <summary>
        /// Retrieves workspaces that are linked to the specified user identifier (ownership or association as returned by the proc).
        /// </summary>
        /// <param name="userId">The ID of the user whose workspaces are requested.</param>
        /// <returns>Returns a list of workspace objects. Empty list if no workspaces match.</returns>
        public static async Task<List<Workspace>> GetWorkspacesByUserId(int userId)
        {
            var list = new List<Workspace>();
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand(SchemaMapping.StoredProcedureNames[SchemaMapping.StoredProcedures.GetUserWorkspaces], connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("userID", userId);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var ws = new Workspace
                {
                    WorkspaceId = GetInt(reader, "WorkspaceID"),
                    ParentGroupId = GetInt(reader, "GroupID"),
                    Name = GetString(reader, "Name"),
                    Description = GetString(reader, "Description")
                };
                list.Add(ws);
            }
            return list;
        }

        /// <summary>
        /// Gets workspaces where the specified user is a member (membership context rather than pure ownership).
        /// </summary>
        /// <param name="userId">The ID of the user whose workspace memberships are requested.</param>
        /// <returns>A list of workspace objects representing membership. Empty if none.</returns>
        public static async Task<List<Workspace>> GetWorkspaceMembershipsByUserId(int userId)
        {
            var list = new List<Workspace>();
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand(SchemaMapping.StoredProcedureNames[SchemaMapping.StoredProcedures.GetUserWorkspaceMemberships], connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("userID", userId);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var ws = new Workspace
                {
                    WorkspaceId = GetInt(reader, "WorkspaceID"),
                    ParentGroupId = GetInt(reader, "GroupID"),
                    Name = GetString(reader, "Name"),
                    Description = GetString(reader, "Description")
                };
                list.Add(ws);
            }
            return list;
        }

        /// <summary>
        /// Fetches child tasks that are directly associated with a given parent task identifier.
        /// </summary>
        /// <param name="taskId">The parent task ID whose child tasks are being queried.</param>
        /// <returns>A list of task objects representing the children. Empty if there are no child tasks.</returns>
        public static async Task<List<Tasks>> GetChildTasksByTaskId(int taskId)
        {
            var list = new List<Tasks>();
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand(SchemaMapping.StoredProcedureNames[SchemaMapping.StoredProcedures.GetChildTasksByTaskId], connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("taskId", taskId);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var task = new Tasks
                {
                    TaskId = GetInt(reader, "ID"),
                    Status = (Schema.WorkingItems.TaskStatus)GetInt(reader, "TaskStatus"),
                    Description = GetString(reader, "Contents"),
                    Title = GetString(reader, "Name"),
                    IsChildTask = true,
                    ParentTaskId = taskId
                };
                list.Add(task);
            }
            return list;
        }

        /// <summary>
        /// Retrieves message objects (comments or chat messages) tied to an owner and narrowed by a message type.
        /// </summary>
        /// <param name="ownerId">The owning entity/user identifier used by the stored procedure.</param>
        /// <param name="type">The message type discriminator (e.g. 1 = TaskComment, 2 = WorkspaceChatMessage).</param>
        /// <returns>Returns a list of IMessage implementations. Empty list if nothing matches the criteria.</returns>
        public static async Task<List<IMessage>> GetMessagesByOwnerAndType(int ownerId, int type)
        {
            var list = new List<IMessage>();
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand(SchemaMapping.StoredProcedureNames[SchemaMapping.StoredProcedures.GetMessagesByOwnerAndType], connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("ownerId", ownerId);
            command.Parameters.AddWithValue("type", type);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                // Assumed column names; adjust if needed.
                var id = GetInt(reader, "ID");
                var content = GetString(reader, "Content");
                var created = GetDateTime(reader, "CreatedAt");
                if (type == (int)MessageType.TaskComment)
                {
                    var taskId = GetInt(reader, "TaskId");
                    list.Add(new TaskComment(id, ownerId, taskId, created, content));
                }
                else if (type == (int)MessageType.WorkspaceChatMessage)
                {
                    var workspaceId = GetInt(reader, "WorkspaceId");
                    list.Add(new WorkspaceChatMessage(id, ownerId, workspaceId, created, content));
                } else
                {
                    throw new Exception($"Unknown message type paramter when getting messages: {type}");
                }
            }
            return list;
        }

        // lightweight result records for auth / registration
        public sealed record RegistrationResult(int ResultCode, string UserId, DateTime AccountCreated);
        public sealed record AuthenticationResult(string UserId, string Username, string Email);

        /// <summary>
        /// Registers a user via stored procedure. Returns null if proc returns no rows.
        /// </summary>
        public static async Task<RegistrationResult?> RegisterUser(string username, string password, string email, DateTime accountCreated)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand(SchemaMapping.StoredProcedureNames[SchemaMapping.StoredProcedures.RegisterUser], connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("Username", username);
            command.Parameters.AddWithValue("Password", password);
            command.Parameters.AddWithValue("Email", email);
            command.Parameters.AddWithValue("AccountCreated", accountCreated);

            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            if (!await reader.ReadAsync().ConfigureAwait(false))
                return null;

            var resultCode = GetInt(reader, "Result");
            var userId = GetString(reader, "UserID");
            var created = GetDateTime(reader, "AccountCreated");

            return new RegistrationResult(resultCode, userId, created);
        }

        /// <summary>
        /// Authenticates a user via stored procedure. Returns null if credentials invalid.
        /// </summary>
        public static async Task<AuthenticationResult?> AuthenticateUser(string username, string password)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand(SchemaMapping.StoredProcedureNames[SchemaMapping.StoredProcedures.AuthenticateUser], connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("Username", username);
            command.Parameters.AddWithValue("Password", password);

            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            if (!await reader.ReadAsync().ConfigureAwait(false))
                return null;

            var userId = GetString(reader, "ID");
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var resolvedUsername = GetString(reader, "Username");
            if (string.IsNullOrWhiteSpace(resolvedUsername))
                resolvedUsername = username;

            var email = GetString(reader, "Email");

            return new AuthenticationResult(userId, resolvedUsername, email);
        }

        // reader helpers
        private static int GetInt(SqlDataReader r, string name)
        {
            try { var ord = r.GetOrdinal(name); return r.IsDBNull(ord) ? 0 : Convert.ToInt32(r.GetValue(ord)); } catch { return 0; }
        }
        private static string GetString(SqlDataReader r, string name)
        {
            try { var ord = r.GetOrdinal(name); return r.IsDBNull(ord) ? string.Empty : r.GetString(ord); } catch { return string.Empty; }
        }
        private static DateTime GetDateTime(SqlDataReader r, string name)
        {
            try { var ord = r.GetOrdinal(name); return r.IsDBNull(ord) ? DateTime.UtcNow : Convert.ToDateTime(r.GetValue(ord)); } catch { return DateTime.UtcNow; }
        }

    }
}
