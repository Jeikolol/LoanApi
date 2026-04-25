using Application.Models.Responses;
using MediatR;

namespace Application.Features.Loans.Queries
{
    public record GetLoanByIdQuery(Guid Id) : IRequest<LoanDetailResponse>;
}
