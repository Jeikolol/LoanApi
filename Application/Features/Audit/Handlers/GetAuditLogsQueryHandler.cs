using Application.Features.Audit.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using System.Data;

namespace Application.Features.Audit.Handlers
{
    public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PaginatedResponse<AuditLogResponse>>
    {
        private const string Filter = @"
            WHERE (@UserPattern IS NULL OR UserName LIKE @UserPattern ESCAPE '\')
              AND (@Action IS NULL OR Action = @Action)
              AND (@From IS NULL OR OccurredOn >= @From)
              AND (@To IS NULL OR OccurredOn < @To)
              AND (@Succeeded IS NULL OR Succeeded = @Succeeded)";

        private readonly IDbConnection _connection;

        public GetAuditLogsQueryHandler(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<PaginatedResponse<AuditLogResponse>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            string sql = $@"
                SELECT Id, OccurredOn, UserId, UserName, Action, Payload, Succeeded, Error, IpAddress, DurationMs
                FROM [Admin].[AuditLog]
                {Filter}
                ORDER BY OccurredOn DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;

                SELECT COUNT(*) FROM [Admin].[AuditLog] {Filter};";

            var args = new
            {
                UserPattern = ToLikePattern(request.User),
                Action = string.IsNullOrWhiteSpace(request.Action) ? null : request.Action.Trim(),
                request.From,
                request.To,
                request.Succeeded,
                Skip = request.GetSkip(),
                Take = request.PageSize,
            };

            using SqlMapper.GridReader grid = await _connection.QueryMultipleAsync(sql, args);
            List<AuditLogResponse> logs = (await grid.ReadAsync<AuditLogResponse>()).ToList();
            int total = await grid.ReadFirstAsync<int>();

            return new PaginatedResponse<AuditLogResponse>(logs, request.PageNumber, request.PageSize, total);
        }

        private static string? ToLikePattern(string? search)
        {
            if (string.IsNullOrWhiteSpace(search)) return null;

            string escaped = search.Trim()
                .Replace(@"\", @"\\")
                .Replace("%", @"\%")
                .Replace("_", @"\_")
                .Replace("[", @"\[");
            return $"%{escaped}%";
        }
    }
}
