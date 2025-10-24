using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Schema;
using TaskHub.Schema.Users;
using TaskHub.Security;

namespace TaskHub.Utility
{
    public static class EndpointMappingUtil
    {
        private delegate Task<IResult> EndpointHandler(HttpContext context);

        public static void MapEndpoints(WebApplication app)
        {
            foreach (var definition in SchemaMapping.ApiDefinitions)
            {
                var handler = CreateHandler(definition);

                var builder = app.MapMethods(definition.Route, new[] { definition.Method.Method }, handler)
                                   .WithName(definition.Endpoint.ToString());

                if (definition.Endpoint is not SchemaMapping.ApiEndpoints.AuthenticateUser
                    && definition.Endpoint is not SchemaMapping.ApiEndpoints.RegisterUser)
                {
                    builder.RequireAuthorization();
                }
            }
        }

        private static EndpointHandler CreateHandler(SchemaMapping.ApiDefinition definition)
        {
            return definition.Endpoint switch
            {
                SchemaMapping.ApiEndpoints.AuthenticateUser => CreateAuthenticationHandler(definition),
                SchemaMapping.ApiEndpoints.RegisterUser => CreateRegistrationHandler(definition),
                _ => CreateDefaultHandler(definition)
            };
        }

        private static EndpointHandler CreateDefaultHandler(SchemaMapping.ApiDefinition definition)
        {
            return async context =>
            {
                var parameterDefinitions = SchemaMapping.StoredProcedureParameters.TryGetValue(definition.Procedure, out var defs)
                    ? defs
                    : Array.Empty<SchemaMapping.StoredProcedureParameter>();

                var parameters = await ParameterBinder.BindAsync(context, parameterDefinitions).ConfigureAwait(false);
                var result = await DbUtil.ExecuteStoredProcedureAsync(definition.Procedure, parameters, context.RequestAborted).ConfigureAwait(false);

                if (definition.ReturnType == typeof(void))
                {
                    return result.Count == 0 ? Results.NoContent() : Results.Ok(result);
                }

                if (TryCoerceResult(result, definition, out var coerced))
                {
                    return Results.Ok(coerced ?? result);
                }

                return Results.BadRequest(new
                {
                    Error = $"Result of stored procedure '{definition.Procedure}' could not be coerced to {definition.ReturnType.Name}."
                });
            };
        }

        private static EndpointHandler CreateAuthenticationHandler(SchemaMapping.ApiDefinition definition)
        {
            return async context =>
            {
                var parameterDefinitions = SchemaMapping.StoredProcedureParameters.TryGetValue(definition.Procedure, out var defs)
                    ? defs
                    : Array.Empty<SchemaMapping.StoredProcedureParameter>();

                var parameters = await ParameterBinder.BindAsync(context, parameterDefinitions).ConfigureAwait(false);
                var result = await DbUtil.ExecuteStoredProcedureAsync(definition.Procedure, parameters, context.RequestAborted).ConfigureAwait(false);

                if (result.Count == 0)
                {
                    return Results.Unauthorized();
                }

                var record = result[0];

                var userId = record.TryGetValue("ID", out var idValue)
                    ? ToStringValue(idValue)
                    : string.Empty;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Results.Unauthorized();
                }

                var username = record.TryGetValue("Username", out var usernameValue)
                    ? ToStringValue(usernameValue)
                    : parameters.TryGetValue("Username", out var usernameParamValue) ? ToStringValue(usernameParamValue) : string.Empty;

                if (string.IsNullOrWhiteSpace(username))
                {
                    return Results.Unauthorized();
                }

                var email = record.TryGetValue("Email", out var emailValue)
                    ? ToStringValue(emailValue)
                    : string.Empty;
                email = string.IsNullOrWhiteSpace(email) ? null : email;

                var tokenService = context.RequestServices.GetRequiredService<IJwtTokenService>();
                var token = tokenService.GenerateToken(userId, username, email);

                return Results.Ok(new LoginResponse
                {
                    Success = true,
                    Token = token
                });
            };
        }

        private static EndpointHandler CreateRegistrationHandler(SchemaMapping.ApiDefinition definition)
        {
            return async context =>
            {
                var parameterDefinitions = SchemaMapping.StoredProcedureParameters.TryGetValue(definition.Procedure, out var defs)
                    ? defs
                    : Array.Empty<SchemaMapping.StoredProcedureParameter>();

                var parameters = await ParameterBinder.BindAsync(context, parameterDefinitions).ConfigureAwait(false);
                var result = await DbUtil.ExecuteStoredProcedureAsync(definition.Procedure, parameters, context.RequestAborted).ConfigureAwait(false);

                if (result.Count == 0)
                {
                    return Results.BadRequest(new { Error = "Registration failed." });
                }

                var record = result[0];
                var resultCode = record.TryGetValue("Result", out var codeValue)
                    ? ToInt(codeValue)
                    : 0;

                if (resultCode <= 0)
                {
                    return Results.Conflict(new { Error = "Username is already in use." });
                }

                if (!record.TryGetValue("UserID", out var userIdValue))
                {
                    return Results.BadRequest(new { Error = "Registration did not return a user identifier." });
                }

                var userId = ToStringValue(userIdValue);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Results.BadRequest(new { Error = "Registration did not produce a valid user identifier." });
                }

                var username = parameters.TryGetValue("Username", out var usernameValue)
                    ? ToStringValue(usernameValue)
                    : string.Empty;

                var email = parameters.TryGetValue("Email", out var emailValue)
                    ? ToStringValue(emailValue)
                    : string.Empty;
                email = string.IsNullOrWhiteSpace(email) ? string.Empty : email;

                var accountCreated = parameters.TryGetValue("AccountCreated", out var accountValue)
                    ? ToDateTime(accountValue)
                    : DateTime.UtcNow;

                var tokenService = context.RequestServices.GetRequiredService<IJwtTokenService>();
                var token = tokenService.GenerateToken(userId, username, email);

                var profile = new UserProfile
                {
                    Username = username,
                    UserID = userId,
                    Email = email,
                    JwtToken = token,
                    AccountCreated = accountCreated
                };

                return Results.Ok(profile);
            };
        }

        private static int ToInt(object? value)
        {
            return value switch
            {
                null => 0,
                int i => i,
                long l => (int)l,
                short s => s,
                decimal dec => (int)dec,
                double dbl => (int)dbl,
                float fl => (int)fl,
                string str when int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
                IFormattable formattable when int.TryParse(formattable.ToString(null, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedFormattable) => parsedFormattable,
                _ => 0
            };
        }

        private static string ToStringValue(object? value)
        {
            return value switch
            {
                null => string.Empty,
                string s => s,
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };
        }

        private static DateTime ToDateTime(object? value)
        {
            return value switch
            {
                DateTime dt => dt,
                string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) => parsed,
                IFormattable formattable when DateTime.TryParse(formattable.ToString(null, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedFormattable) => parsedFormattable,
                _ => DateTime.UtcNow
            };
        }

        private static bool TryCoerceResult(object result, SchemaMapping.ApiDefinition definition, out object? coerced)
        {
            var targetType = definition.ReturnType;

            if (definition.ReturnsCollection)
            {
                var listType = typeof(List<>).MakeGenericType(targetType);
                return TryDeserialize(result, listType, out coerced);
            }

            if (targetType.IsInstanceOfType(result))
            {
                coerced = result;
                return true;
            }

            return TryDeserialize(result, targetType, out coerced);
        }

        private static bool TryDeserialize(object result, Type targetType, out object? coerced)
        {
            try
            {
                var json = JsonSerializer.Serialize(result);
                coerced = JsonSerializer.Deserialize(json, targetType);
                return coerced is not null || targetType == typeof(string);
            }
            catch
            {
                coerced = null;
                return false;
            }
        }

        private static class ParameterBinder
        {
            public static async Task<IReadOnlyDictionary<string, object?>> BindAsync(HttpContext context, IReadOnlyList<SchemaMapping.StoredProcedureParameter> expected)
            {
                var buffer = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                if (expected.Count == 0)
                    return buffer;

                foreach (var routeValue in context.Request.RouteValues)
                {
                    if (string.IsNullOrWhiteSpace(routeValue.Key) || routeValue.Value is null)
                        continue;

                    var key = Normalize(routeValue.Key);
                    buffer[key] = routeValue.Value;
                }

                foreach (var queryPair in context.Request.Query)
                {
                    if (string.IsNullOrWhiteSpace(queryPair.Key))
                        continue;

                    var key = Normalize(queryPair.Key);
                    buffer[key] = queryPair.Value.Count switch
                    {
                        0 => null,
                        1 => queryPair.Value[0],
                        _ => queryPair.Value.ToArray()
                    };
                }

                if (context.Request.ContentLength > 0 && context.Request.Body.CanRead && IsJson(context.Request.ContentType))
                {
                    context.Request.EnableBuffering();
                    try
                    {
                        using var document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted).ConfigureAwait(false);
                        context.Request.Body.Position = 0;

                        if (document.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var property in document.RootElement.EnumerateObject())
                            {
                                var key = Normalize(property.Name);
                                buffer[key] = ExtractJsonValue(property.Value);
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        context.Request.Body.Position = 0;
                    }
                }

                var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var definition in expected)
                {
                    if (!buffer.TryGetValue(definition.Name, out var raw))
                    {
                        normalized[definition.Name] = GetDefault(definition.Type);
                        continue;
                    }

                    normalized[definition.Name] = ConvertValue(raw, definition.Type);
                }

                return normalized;
            }

            private static string Normalize(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return key;

                key = key.TrimStart('@');
                return key.Length == 1
                    ? key.ToUpperInvariant()
                    : char.ToUpperInvariant(key[0]) + key[1..];
            }

            private static object? ConvertValue(object? value, Type targetType)
            {
                if (value is null)
                {
                    return GetDefault(targetType);
                }

                if (value is JsonElement json)
                    return ExtractJsonValue(json);

                if (targetType == typeof(int))
                {
                    if (value is int i) return i;
                    return int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
                }

                if (targetType == typeof(DateTime))
                {
                    if (value is DateTime dt) return dt;
                    return DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                        ? parsed
                        : DateTime.UtcNow;
                }

                if (targetType == typeof(bool))
                {
                    if (value is bool b) return b;
                    return bool.TryParse(value.ToString(), out var parsed) && parsed;
                }

                if (targetType == typeof(string))
                {
                    return value.ToString();
                }

                if (targetType == typeof(Guid))
                {
                    if (value is Guid guid) return guid;
                    return Guid.TryParse(value.ToString(), out var parsed) ? parsed : Guid.Empty;
                }

                return value;
            }

            private static object? ExtractJsonValue(JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number when element.TryGetInt64(out var l) => l,
                    JsonValueKind.Number when element.TryGetDouble(out var d) => d,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    JsonValueKind.Array => element.EnumerateArray().Select(ExtractJsonValue).ToArray(),
                    JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ExtractJsonValue(p.Value), StringComparer.OrdinalIgnoreCase),
                    _ => element.GetRawText()
                };
            }

            private static bool IsJson(string? contentType) => contentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;

            private static object? GetDefault(Type targetType)
            {
                if (targetType == typeof(DateTime))
                {
                    return DateTime.UtcNow;
                }

                if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null)
                {
                    return null;
                }

                return Activator.CreateInstance(targetType);
            }
        }
    }
}
