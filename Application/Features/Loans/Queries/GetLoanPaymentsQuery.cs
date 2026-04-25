using Application.Common.Filters;
using Application.Models.Responses;
using MediatR;

namespace Application.Features.Loans.Queries
{
    public class GetLoanPaymentsQuery : PaginationFilter, IRequest<PaginatedResponse<LoanPaymentResponse>>
    {
        public Guid LoanId { get; set; }

        public GetLoanPaymentsQuery() { }

        public GetLoanPaymentsQuery(Guid loanId, int pageNumber = 1, int pageSize = 10)
            : base(pageNumber, pageSize)
        {
            LoanId = loanId;
        }
    }
}
