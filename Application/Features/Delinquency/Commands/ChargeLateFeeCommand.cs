using Application.Models.Responses;
using MediatR;

namespace Application.Features.Delinquency.Commands
{
    /// <summary>Charges the late fees of the overdue days that have not been charged yet. Safe to repeat: a day is charged once.</summary>
    public record ChargeLateFeeCommand(Guid LoanId) : IRequest<LateFeeChargeResponse>;
}
