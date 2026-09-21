using Application.Features.Loans.Commands;
using MediatR;

namespace Application.Features.Delinquency.Commands
{
    /// <summary>Writes off a loan that is past the policy's write-off period, keeping the reason for the audit.</summary>
    public class WriteOffLoanCommand : IRequest<LoanTransitionResponse>
    {
        public Guid LoanId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
