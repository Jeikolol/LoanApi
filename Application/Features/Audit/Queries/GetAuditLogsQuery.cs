using Application.Common.Filters;
using Application.Models.Responses;
using MediatR;

namespace Application.Features.Audit.Queries
{
    public class GetAuditLogsQuery : PaginationFilter, IRequest<PaginatedResponse<AuditLogResponse>>
    {
        public string? User { get; set; }
        public string? Action { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public bool? Succeeded { get; set; }

        public GetAuditLogsQuery() { }

        public GetAuditLogsQuery(int pageNumber, int pageSize) : base(pageNumber, pageSize) { }
    }
}
