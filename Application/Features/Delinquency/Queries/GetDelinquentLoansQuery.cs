using Application.Common.Filters;
using Application.Models.Responses;
using MediatR;

namespace Application.Features.Delinquency.Queries
{
    public class GetDelinquentLoansQuery : PaginationFilter, IRequest<PaginatedResponse<LoanSummaryResponse>>
    {
    }
}
