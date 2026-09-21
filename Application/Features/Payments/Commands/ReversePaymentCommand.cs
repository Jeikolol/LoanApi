using Application.Models.Responses;
using MediatR;

namespace Application.Features.Payments.Commands
{
    /// <summary>
    /// Undoes a completed payment (a mistake, a bounced cheque). The payment stays in the history as Reversed with
    /// its reason; the loan's balances and schedule go back to what they were without it.
    /// </summary>
    public class ReversePaymentCommand : IRequest<LoanPaymentResponse>
    {
        public Guid Id { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
