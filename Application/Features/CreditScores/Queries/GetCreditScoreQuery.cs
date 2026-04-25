using Application.Models.Responses;
using MediatR;

namespace Application.Features.CreditScores.Queries
{
    public record GetCreditScoreQuery(Guid CustomerId) : IRequest<CreditScoreResponse>;
}
