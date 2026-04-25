using Application.Models.Responses;
using MediatR;

namespace Application.Features.Loans.Queries
{
    public record GetDashboardSummaryQuery : IRequest<LoanDashboardSummaryResponse>;
}
