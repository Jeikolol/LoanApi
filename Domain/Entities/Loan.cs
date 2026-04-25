using Domain.Enums;

namespace Domain.Entities
{
    public class Loan : AuditableEntity<Guid>
    {
        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; } = default!;

        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = default!;

        public Guid CurrencyId { get; set; }
        public Currency Currency { get; set; } = default!;

        public string LoanNumber { get; set; } = string.Empty;

        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public int TermMonths { get; set; }

        public decimal InstallmentAmount { get; set; }
        public LoanStatus LoanStatus { get; set; }

        public DateOnly DisbursementDate { get; set; }
        public DateOnly MaturityDate { get; set; }
        public DateOnly NextPaymentDueDate { get; set; }

        // Delinquency tracking (crucial for Dominican market)
        public int DaysOverdue { get; set; } = 0;
        public decimal DelinquencyPercentage { get; set; } = 0m;
        public DateTime? LastPaymentDate { get; set; }

        // Interest calculations
        public decimal TotalInterestAmount { get; set; }
        public decimal PaidInterest { get; set; }
        public decimal RemainingInterest { get; set; }

        // Balance tracking
        public decimal AmortizationBalance { get; set; }
        public decimal PrincipalPaid { get; set; }
        public decimal PrincipalRemaining { get; set; }

        // Dominican bank requirements
        public LoanPurpose? LoanPurpose { get; set; }
        public decimal MaxCredit { get; set; }
        public bool RequiresGuarantor { get; set; } = false;
        public Guid? GuarantorId { get; set; }

        // Collections
        public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();
        public ICollection<LoanDocument> Documents { get; set; } = new List<LoanDocument>();
    }

}
