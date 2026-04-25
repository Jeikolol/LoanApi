using Application.Common.Filters;
using Application.Models.Responses;
using MediatR;

namespace Application.Features.Documents.Queries
{
    public class GetDocumentsByLoanQuery : PaginationFilter, IRequest<PaginatedResponse<LoanDocumentResponse>>
    {
        public Guid LoanId { get; set; }

        public GetDocumentsByLoanQuery() { }

        public GetDocumentsByLoanQuery(Guid loanId, int pageNumber = 1, int pageSize = 10)
            : base(pageNumber, pageSize)
        {
            LoanId = loanId;
        }
    }
}
