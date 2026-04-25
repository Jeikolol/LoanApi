using Domain.Enums;

namespace Domain.Entities
{
    public class LoanPayment : AuditableEntity<Guid>
    {
        public Guid LoanId { get; set; }
        public Loan Loan { get; set; } = default!;

        public Guid CurrencyId { get; set; }
        public Currency Currency { get; set; } = default!;

        public DateOnly PaymentDate { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PenaltyAmount { get; set; }
        public decimal TotalPaymentAmount { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;
        public string? ReferenceNumber { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Completed;
    }
}
