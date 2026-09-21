using Application.Interfaces;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Application.Behaviors
{
    /// <summary>
    /// Records every command (any request whose type name ends in "Command") in the audit log:
    /// who ran it, when, the request data (secrets redacted) and whether it succeeded.
    /// </summary>
    public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private const int MaxPayloadLength = 4000;
        private const int MaxErrorLength = 500;
        private const int MaxStringValueLength = 200;

        private static readonly string[] SecretKeyFragments = ["password", "token", "secret"];

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IDbConnection _connection;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

        public AuditBehavior(IDbConnection connection, ICurrentUser currentUser, ILogger<AuditBehavior<TRequest, TResponse>> logger)
        {
            _connection = connection;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!typeof(TRequest).Name.EndsWith("Command", StringComparison.Ordinal))
            {
                return await next(cancellationToken);
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                TResponse response = await next(cancellationToken);
                await Record(request, succeeded: true, error: null, stopwatch.Elapsed);
                return response;
            }
            catch (Exception ex)
            {
                await Record(request, succeeded: false, error: ex.Message, stopwatch.Elapsed);
                throw;
            }
        }

        private async Task Record(TRequest request, bool succeeded, string? error, TimeSpan elapsed)
        {
            string action = typeof(TRequest).Name;

            try
            {
                JsonNode? payload = JsonSerializer.SerializeToNode(request, request.GetType(), SerializerOptions);
                Scrub(payload);

                string? userName = _currentUser.UserName ?? (payload as JsonObject)?["UserName"]?.GetValue<string>()
                    ?? (payload as JsonObject)?["userName"]?.GetValue<string>();

                await _connection.ExecuteAsync(
                    @"INSERT INTO [Admin].[AuditLog]
                        (Id, OccurredOn, UserId, UserName, Action, Payload, Succeeded, Error, IpAddress, DurationMs)
                      VALUES
                        (@Id, @OccurredOn, @UserId, @UserName, @Action, @Payload, @Succeeded, @Error, @IpAddress, @DurationMs)",
                    new
                    {
                        Id = Guid.NewGuid(),
                        OccurredOn = DateTime.UtcNow,
                        UserId = _currentUser.UserId,
                        UserName = Truncate(userName, 100),
                        Action = action,
                        Payload = Truncate(payload?.ToJsonString(), MaxPayloadLength),
                        Succeeded = succeeded,
                        Error = Truncate(error, MaxErrorLength),
                        IpAddress = Truncate(_currentUser.IpAddress, 64),
                        DurationMs = (int)elapsed.TotalMilliseconds,
                    });
            }
            catch (Exception ex)
            {
                // The operation itself already finished; never turn a business result into a failure because auditing failed.
                _logger.LogError(ex, "Failed to write the audit log entry for {Action}", action);
            }
        }

        private static void Scrub(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject obj:
                    foreach (string key in obj.Select(p => p.Key).ToList())
                    {
                        if (SecretKeyFragments.Any(f => key.Contains(f, StringComparison.OrdinalIgnoreCase)))
                        {
                            obj[key] = "***";
                        }
                        else if (obj[key] is JsonValue value && value.TryGetValue(out string? text) && text.Length > MaxStringValueLength)
                        {
                            obj[key] = $"[{text.Length} characters]";
                        }
                        else
                        {
                            Scrub(obj[key]);
                        }
                    }

                    break;

                case JsonArray array:
                    foreach (JsonNode? item in array)
                    {
                        Scrub(item);
                    }

                    break;
            }
        }

        private static string? Truncate(string? value, int max) =>
            value is null || value.Length <= max ? value : value[..max];
    }
}
