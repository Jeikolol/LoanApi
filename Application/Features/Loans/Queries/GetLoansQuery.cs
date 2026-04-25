using Application.Common.Filters;
using Application.Models.Responses;
using MediatR;

namespace Application.Features.Loans.Queries
{
    public class GetLoansQuery : PaginationFilter, IRequest<PaginatedResponse<LoanSummaryResponse>>
    {
    }
}
