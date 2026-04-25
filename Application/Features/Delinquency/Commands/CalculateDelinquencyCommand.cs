using Application.Models.Responses;
using MediatR;

namespace Application.Features.Delinquency.Commands
{
    public record CalculateDelinquencyCommand(Guid LoanId) : IRequest<DelinquencyStatusResponse>;
}
