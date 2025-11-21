global using static TaskHub.Schema.SchemaMapping;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using TaskHub.Schema;
using TaskHub.Schema.WorkingItems;
using TaskHub.Security;
using TaskHub.Utility;

namespace TaskHub
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables();
            });

            builder.Logging.AddFilter("Microsoft", LogLevel.None);

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            });

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
            });

            builder.Services
                .AddOptions<JwtOptions>()
                .BindConfiguration("Jwt");
            builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var secret = builder.Configuration["Jwt:Key"];

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();
            
            DbUtil.Initialize(app.Configuration);

            ///<summary>
            /// This endpoint returns a List<Tasks>
            /// So in terms of JSON, put the Tasks object into a JSON formatter and then make it a list and that's what's expected in JavaScript
            /// 
            /// JSON to C# source 1: https://jsontotable.org/csharp-to-json
            /// JSON to C# source 2: https://csharp2json.azurewebsites.net/
            /// </summary>
            /// 
            /// ? seriously just copy this code block for an example of the expected JSON format ?
            /// 
            /// <code>
            ///    var tasks = new List<Tasks>
            ///    {
            ///        new Tasks
            ///        {
            ///            TaskId = 1,
            ///            Title = "first task",
            ///            Description = "do first thing",
            ///            DueDate = DateTime.UtcNow.AddDays(7),
            ///            Status = TaskStatus.InProgress,
            ///            IsChildTask = false
            ///        },
            ///        new Tasks
            ///        {
            ///            TaskId = 2,
            ///            Title = "second task",
            ///            Description = "do second things",
            ///            DueDate = DateTime.UtcNow.AddDays(2),
            ///            Status = TaskStatus.Pending,
            ///            IsChildTask = true,
            ///            ParentTaskId = 1
            ///        }
            ///    };
            ///
            /// string json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions
            /// {
            ///     WriteIndented = true
            /// });
            ///
            /// Console.WriteLine(json);
            /// </code>
            app.MapPut(ApiEndpointNames[ApiEndpoints.GetUserTasks], async Task<IResult> (HttpContext context) =>
            {
                // read the PUT body (not used yet, but keeping it here for consistency)
                using var reader = new StreamReader(context.Request.Body);
                string bodyJson = await reader.ReadToEndAsync();

                var userId = context.Request.RouteValues["userId"]; // grab userId from the route

                if(Int32.TryParse(userId?.ToString(), out int userIdInt) == false) // make sure it's a valid int
                {
                    return Results.BadRequest("Invalid userId parameter.");
                }

                var tasks = await DbUtil.GetListTaskWithUserId(userIdInt); // go get tasks for this user

                var taskout = JsonSerializer.Serialize<List<Tasks>>(tasks); // serialize tasks to JSON

                if(taskout == null) // sanity check
                {
                    return Results.BadRequest("No tasks found for the given userId.");
                }

                return Results.Content(taskout); // ship it
            }).RequireAuthorization();

            ///<summary>
            /// This endpoint returns a List<Tasks>
            /// So in terms of JSON, put the Tasks object into a JSON formatter and then make it a list and that's what's expected in JavaScript
            /// </summary>
            app.MapPut(ApiEndpointNames[ApiEndpoints.GetWorkspaceTasks], async (HttpContext context) =>
            {
                // read body (keeping pattern)
                using var bodyReader = new StreamReader(context.Request.Body);
                string _ = await bodyReader.ReadToEndAsync();

                var workspaceIdRaw = context.Request.RouteValues["workspaceId"]; // grab workspaceId from route
                if (Int32.TryParse(workspaceIdRaw?.ToString(), out int workspaceId) == false) // validate
                {
                    return Results.BadRequest("Invalid workspaceId parameter.");
                }

                var tasks = await DbUtil.GetTasksByWorkspaceId(workspaceId); // get tasks tied to workspace
                var json = JsonSerializer.Serialize<List<Tasks>>(tasks); // serialize list

                if (json == null) // should never be null
                {
                    return Results.BadRequest("No tasks found for the given workspaceId.");
                }

                return Results.Content(json); // return raw JSON string
            }).RequireAuthorization();

            ///<summary>
            /// This endpoint returns a List<Workspace>
            /// So in terms of JSON, put the Workspace object into a JSON formatter and then make it a list and that's what's expected in JavaScript
            /// </summary>
            app.MapPut(ApiEndpointNames[ApiEndpoints.GetGroupWorkspaces], async (HttpContext context) =>
            {
                using var bodyReader = new StreamReader(context.Request.Body);
                string _ = await bodyReader.ReadToEndAsync();

                var groupIdRaw = context.Request.RouteValues["groupId"]; // grab groupId
                if (Int32.TryParse(groupIdRaw?.ToString(), out int groupId) == false)
                {
                    return Results.BadRequest("Invalid groupId parameter.");
                }

                var workspaces = await DbUtil.GetWorkspacesByGroupId(groupId); // pull workspaces for group
                var json = JsonSerializer.Serialize<List<Workspace>>(workspaces);

                if (json == null)
                {
                    return Results.BadRequest("No workspaces found for the given groupId.");
                }

                return Results.Content(json);
            }).RequireAuthorization();

            ///<summary>
            /// This endpoint returns a List<Group>
            /// So in terms of JSON, put the Group object into a JSON formatter and then make it a list and that's what's expected in JavaScript
            /// </summary>
            app.MapPut(ApiEndpointNames[ApiEndpoints.GetUserGroups], async (HttpContext context) =>
            {
                using var bodyReader = new StreamReader(context.Request.Body);
                string _ = await bodyReader.ReadToEndAsync();

                var userIdRaw = context.Request.RouteValues["userId"]; // grab userId
                if (Int32.TryParse(userIdRaw?.ToString(), out int userId) == false)
                {
                    return Results.BadRequest("Invalid userId parameter.");
                }

                var groups = await DbUtil.GetGroupsByUserId(userId); // get groups for user
                var json = JsonSerializer.Serialize<List<Group>>(groups);

                if (json == null)
                {
                    return Results.BadRequest("No groups found for the given userId.");
                }

                return Results.Content(json);
            }).RequireAuthorization();

            ///<summary>
            /// This endpoint returns a List<Workspace>
            /// So in terms of JSON, put the Workspace object into a JSON formatter and then make it a list and that's what's expected in JavaScript
            /// </summary>
            app.MapPut(ApiEndpointNames[ApiEndpoints.GetUserWorkspaces], async (HttpContext context) =>
            {
                using var bodyReader = new StreamReader(context.Request.Body);
                string _ = await bodyReader.ReadToEndAsync();

                var userIdRaw = context.Request.RouteValues["userId"]; // route userId
                if (Int32.TryParse(userIdRaw?.ToString(), out int userId) == false)
                {
                    return Results.BadRequest("Invalid userId parameter.");
                }

                var workspaces = await DbUtil.GetWorkspacesByUserId(userId); // fetch workspaces
                var json = JsonSerializer.Serialize<List<Workspace>>(workspaces);

                if (json == null)
                {
                    return Results.BadRequest("No workspaces found for the given userId.");
                }

                return Results.Content(json);
            }).RequireAuthorization();

            ///<summary>
            /// This endpoint returns a List<Workspace>
            /// So in terms of JSON, put the Workspace object into a JSON formatter and then make it a list and that's what's expected in JavaScript
            /// </summary>
            app.MapPut(ApiEndpointNames[ApiEndpoints.GetUserWorkspaceMemberships], async (HttpContext context) =>
            {
                using var bodyReader = new StreamReader(context.Request.Body);
                string _ = await bodyReader.ReadToEndAsync();

                var userIdRaw = context.Request.RouteValues["userId"]; // route param
                if (Int32.TryParse(userIdRaw?.ToString(), out int userId) == false)
                {
                    return Results.BadRequest("Invalid userId parameter.");
                }

                var memberships = await DbUtil.GetWorkspaceMembershipsByUserId(userId); // get memberships
                var json = JsonSerializer.Serialize<List<Workspace>>(memberships);

                if (json == null)
                {
                    return Results.BadRequest("No workspace memberships found for the given userId.");
                }

                return Results.Content(json);
            }).RequireAuthorization();

            ///<summary>
            /// This endpoint returns a List<Tasks>
            /// So in terms of JSON, put the Tasks object into a JSON formatter and then make it a list and that's what's expected in JavaScript
            /// </summary>
            app.MapPut(ApiEndpointNames[ApiEndpoints.GetChildTasksByTaskId], async (HttpContext context) =>
            {
                using var bodyReader = new StreamReader(context.Request.Body);
                string _ = await bodyReader.ReadToEndAsync();

                var taskIdRaw = context.Request.RouteValues["taskId"]; // grab taskId
                if (Int32.TryParse(taskIdRaw?.ToString(), out int taskId) == false)
                {
                    return Results.BadRequest("Invalid taskId parameter.");
                }

                var children = await DbUtil.GetChildTasksByTaskId(taskId); // get child tasks
                var json = JsonSerializer.Serialize<List<Tasks>>(children);

                if (json == null)
                {
                    return Results.BadRequest("No child tasks found for the given taskId.");
                }

                return Results.Content(json);
            }).RequireAuthorization();

            ///<summary>
            /// This endpoint returns a List<IMessage>
            /// So in terms of JSON, put the IMessage interface into a JSON formatter and then make it a list and that's what's expected in JavaScript
            /// </summary>
            app.MapPut(ApiEndpointNames[ApiEndpoints.GetMessagesByOwnerAndType], async (HttpContext context) =>
            {
                using var bodyReader = new StreamReader(context.Request.Body);
                string _ = await bodyReader.ReadToEndAsync();

                var ownerIdRaw = context.Request.Query["ownerId"].FirstOrDefault(); // ownerId from query
                var typeRaw = context.Request.Query["type"].FirstOrDefault(); // type from query

                if (Int32.TryParse(ownerIdRaw, out int ownerId) == false)
                {
                    return Results.BadRequest("Invalid ownerId query parameter.");
                }
                if (Int32.TryParse(typeRaw, out int type) == false)
                {
                    return Results.BadRequest("Invalid type query parameter.");
                }

                var messages = await DbUtil.GetMessagesByOwnerAndType(ownerId, type); // grab messages
                var json = JsonSerializer.Serialize<List<IMessage>>(messages);

                if (json == null)
                {
                    return Results.BadRequest("No messages found for the given ownerId/type.");
                }

                return Results.Content(json);
            }).RequireAuthorization();

            app.UseDefaultFiles(); // Serves index.html by default
            app.UseStaticFiles();
            // Add CORS
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            //EndpointMappingUtil.MapEndpoints(app);

            app.Run();
        }
    }

    public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

    [JsonSerializable(typeof(Todo[]))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {

    }
}
