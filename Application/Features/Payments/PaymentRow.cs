using Application.Models.Responses;
using Domain.Enums;

namespace Application.Features.Payments
{
    internal class PaymentRow
    {
        public Guid Id { get; set; }
        public Guid LoanId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PenaltyAmount { get; set; }
        public decimal TotalPaymentAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? ReversalReason { get; set; }
        public string? RegisteredBy { get; set; }

        public LoanPaymentResponse ToResponse() => new(
            Id, LoanId, PaymentDate, PrincipalAmount, InterestAmount, PenaltyAmount,
            TotalPaymentAmount, PaymentMethod.ToString(), ReferenceNumber, Status, CreatedOn, ReversalReason, RegisteredBy);
    }
}
