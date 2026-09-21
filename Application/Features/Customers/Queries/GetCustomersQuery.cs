using Application.Common.Filters;
using Application.Models.Responses;
using MediatR;

namespace Application.Features.Customers.Queries
{
    public class GetCustomersQuery : PaginationFilter, IRequest<PaginatedResponse<CustomerSummaryResponse>>
    {
        /// <summary>Matches part of the name, company, identification or phone.</summary>
        public string? Search { get; set; }
    }
}
