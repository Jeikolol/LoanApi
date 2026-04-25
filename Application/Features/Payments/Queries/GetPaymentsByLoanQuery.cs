using Application.Common.Filters;
using Application.Models.Responses;
using MediatR;

namespace Application.Features.Payments.Queries
{
    public class GetPaymentsByLoanQuery : PaginationFilter, IRequest<PaginatedResponse<LoanPaymentResponse>>
    {
        public Guid LoanId { get; set; }

        public GetPaymentsByLoanQuery() { }

        public GetPaymentsByLoanQuery(Guid loanId, int pageNumber = 1, int pageSize = 10)
            : base(pageNumber, pageSize)
        {
            LoanId = loanId;
        }
    }
}
