using Application.Models.Responses;
using MediatR;

namespace Application.Features.Delinquency.Queries
{
    public record GetDelinquencyStatusQuery(Guid LoanId) : IRequest<DelinquencyStatusResponse>;
}
