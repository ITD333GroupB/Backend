using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using TaskHub.Schema.Users;
using TaskHub.Schema.WorkingItems;

namespace TaskHub.Schema
{
    public static class SchemaMapping
    {
        public record StoredProcedureParameter(string Name, Type Type);
        public record ApiDefinition(ApiEndpoints Endpoint, string Route, HttpMethod Method, StoredProcedures Procedure, Type ReturnType, bool ReturnsCollection);

        public enum ApiEndpoints
        {
            GetWorkspaceTasks,
            GetGroupWorkspaces,
            GetUserGroups,
            GetUserWorkspaces,
            GetUserTasks,
            GetUserWorkspaceMemberships,
            GetMessagesByOwnerAndType,
            GetChildTasksByTaskId,
            RegisterUser,
            AuthenticateUser
        }

        public enum StoredProcedures
        {
            GetWorkspaceTasks,
            GetGroupWorkspaces,
            GetUserGroups,
            GetUserWorkspaces,
            GetUserTasks,
            GetUserWorkspaceMemberships,
            GetMessagesByOwnerAndType,
            GetChildTasksByTaskId,
            RegisterUser,
            AuthenticateUser
        }

        public static readonly ApiDefinition[] ApiDefinitions =
        {
            new(ApiEndpoints.GetWorkspaceTasks,         "/api/workspaces/{workspaceId}/tasks", HttpMethod.Get,          StoredProcedures.GetWorkspaceTasks,     typeof(List<Tasks>), true),
            new(ApiEndpoints.GetGroupWorkspaces,        "/api/groups/{groupId}/workspaces", HttpMethod.Get,             StoredProcedures.GetGroupWorkspaces,    typeof(List<Workspace>), true),
            new(ApiEndpoints.GetUserGroups,             "/api/users/{userId}/groups",       HttpMethod.Get,             StoredProcedures.GetUserGroups,         typeof(List<Group>), true),
            new(ApiEndpoints.GetUserWorkspaces,         "/api/users/{userId}/workspaces",   HttpMethod.Get,             StoredProcedures.GetUserWorkspaces,     typeof(List<Workspace>), true),
            new(ApiEndpoints.GetUserTasks,              "/api/users/{userId}/tasks",        HttpMethod.Get,             StoredProcedures.GetUserTasks,          typeof(List<Tasks>), true),
            new(ApiEndpoints.GetUserWorkspaceMemberships, "/api/users/{userId}/workspace-memberships", HttpMethod.Get,  StoredProcedures.GetUserWorkspaceMemberships,   typeof(List<Workspace>), true),
            new(ApiEndpoints.GetMessagesByOwnerAndType, "/api/messages",                    HttpMethod.Get,             StoredProcedures.GetMessagesByOwnerAndType,     typeof(List<IMessage>), true),
            new(ApiEndpoints.GetChildTasksByTaskId,     "/api/tasks/{taskId}/children",     HttpMethod.Get,             StoredProcedures.GetChildTasksByTaskId, typeof(List<Tasks>), true),
            new(ApiEndpoints.RegisterUser,              "/api/auth/register",               HttpMethod.Post,            StoredProcedures.RegisterUser,          typeof(UserProfile), false),
            new(ApiEndpoints.AuthenticateUser,          "/api/auth/login",                  HttpMethod.Post,            StoredProcedures.AuthenticateUser,      typeof(LoginResponse), false)
        };

        public static readonly Dictionary<ApiEndpoints, StoredProcedures> ApiToStoredProcedureMap = ApiDefinitions.ToDictionary(d => d.Endpoint, d => d.Procedure);

        public static readonly Dictionary<ApiEndpoints, string> ApiEndpointNames = ApiDefinitions.ToDictionary(d => d.Endpoint, d => d.Route);

        public static readonly Dictionary<ApiEndpoints, HttpMethod> ApiEndpointHttpMethods = ApiDefinitions.ToDictionary(d => d.Endpoint, d => d.Method);

        public static readonly Dictionary<ApiEndpoints, Type> ApiReturnTypes = ApiDefinitions.ToDictionary(d => d.Endpoint, d => d.ReturnType);

        public static readonly Dictionary<ApiEndpoints, bool> ApiReturnsCollections = ApiDefinitions.ToDictionary(d => d.Endpoint, d => d.ReturnsCollection);

        public static readonly Dictionary<StoredProcedures, string> StoredProcedureNames = new Dictionary<StoredProcedures, string>()
        {
            { StoredProcedures.GetWorkspaceTasks, "GetWorkspaceTasks" },
            { StoredProcedures.GetGroupWorkspaces, "GetGroupWorkspaces" },
            { StoredProcedures.GetUserGroups, "GetUserGroups" },
            { StoredProcedures.GetUserWorkspaces, "GetUserWorkspaces" },
            { StoredProcedures.GetUserTasks, "GetUserTasks" },
            { StoredProcedures.GetUserWorkspaceMemberships, "GetUserWorkspaceMemberships" },
            { StoredProcedures.GetMessagesByOwnerAndType, "GetMessagesByOwnerAndType" },
            { StoredProcedures.GetChildTasksByTaskId, "GetChildTasksByTaskId" },
            { StoredProcedures.RegisterUser, "RegisterUser" },
            { StoredProcedures.AuthenticateUser, "AuthenticateUser" }
        };

        public static readonly Dictionary<ApiEndpoints, string[]> ApiEndpointQueryParameters = new Dictionary<ApiEndpoints, string[]>()
        {
            { ApiEndpoints.GetUserTasks, new[] { "userId" } }
        };

        public static readonly Dictionary<StoredProcedures, StoredProcedureParameter[]> StoredProcedureParameters = new()
        {
            { StoredProcedures.GetWorkspaceTasks, new[] { new StoredProcedureParameter("workspaceID", typeof(int)) } },
            { StoredProcedures.GetGroupWorkspaces, new[] { new StoredProcedureParameter("groupID", typeof(int)) } },
            { StoredProcedures.GetUserGroups, new[] { new StoredProcedureParameter("userID", typeof(int)) } },
            { StoredProcedures.GetUserWorkspaces, new[] { new StoredProcedureParameter("userID", typeof(int)) } },
            { StoredProcedures.GetUserTasks, new[] { new StoredProcedureParameter("userID", typeof(int)) } },
            { StoredProcedures.GetUserWorkspaceMemberships, new[] { new StoredProcedureParameter("userID", typeof(int)) } },
            { StoredProcedures.GetMessagesByOwnerAndType, new[] { new StoredProcedureParameter("ownerId", typeof(int)), new StoredProcedureParameter("type", typeof(int)) } },
            { StoredProcedures.GetChildTasksByTaskId, new[] { new StoredProcedureParameter("taskId", typeof(int)) } },
            { StoredProcedures.RegisterUser, new[] {
                new StoredProcedureParameter("Username", typeof(string)),
                new StoredProcedureParameter("Password", typeof(string)),
                new StoredProcedureParameter("Email", typeof(string)),
                new StoredProcedureParameter("AccountCreated", typeof(DateTime))
            } },
            { StoredProcedures.AuthenticateUser, new[] {
                new StoredProcedureParameter("Username", typeof(string)),
                new StoredProcedureParameter("Password", typeof(string))
            } }
        };
    }
}
