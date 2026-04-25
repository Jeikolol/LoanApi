using Application.Common.Filters;
using Application.Models.Responses;
using MediatR;

namespace Application.Features.Loans.Queries
{
    public class GetLoansByCustomerQuery : PaginationFilter, IRequest<PaginatedResponse<LoanSummaryResponse>>
    {
        public Guid CustomerId { get; set; }

        public GetLoansByCustomerQuery() { }

        public GetLoansByCustomerQuery(Guid customerId, int pageNumber = 1, int pageSize = 10)
            : base(pageNumber, pageSize)
        {
            CustomerId = customerId;
        }
    }
}
