using Application.Models.Responses;
using MediatR;

namespace Application.Features.CreditScores.Commands
{
    public record RecalculateCreditScoreCommand(Guid CustomerId) : IRequest<CreditScoreResponse>;
}
