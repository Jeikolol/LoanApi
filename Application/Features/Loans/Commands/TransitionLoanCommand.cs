using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Commands
{
    public record LoanTransitionResponse(Guid LoanId, string LoanNumber, LoanStatus LoanStatus);

    /// <summary>Approve, disburse, cancel or close a loan.</summary>
    public class TransitionLoanCommand : IRequest<LoanTransitionResponse>
    {
        public Guid LoanId { get; set; }
        public LoanAction Action { get; set; }
    }
}
