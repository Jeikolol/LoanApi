using Application.Models.Responses;
using Domain.Enums;
using MediatR;

namespace Application.Features.Payments.Commands
{
    public class CreatePaymentCommand : IRequest<LoanPaymentResponse>
    {
        public Guid LoanId { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PenaltyAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
    }
}
