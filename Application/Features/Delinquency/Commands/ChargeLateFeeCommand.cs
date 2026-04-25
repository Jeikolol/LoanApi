using Application.Models.Responses;
using MediatR;

namespace Application.Features.Delinquency.Commands
{
    public record ChargeLateFeeCommand(Guid LoanId) : IRequest<DelinquencyStatusResponse>;
}
