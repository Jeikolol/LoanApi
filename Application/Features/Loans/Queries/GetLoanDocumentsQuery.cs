using Application.Common.Filters;
using Application.Models.Responses;
using MediatR;

namespace Application.Features.Loans.Queries
{
    public class GetLoanDocumentsQuery : PaginationFilter, IRequest<PaginatedResponse<LoanDocumentResponse>>
    {
        public Guid LoanId { get; set; }

        public GetLoanDocumentsQuery() { }

        public GetLoanDocumentsQuery(Guid loanId, int pageNumber = 1, int pageSize = 10)
            : base(pageNumber, pageSize)
        {
            LoanId = loanId;
        }
    }
}
