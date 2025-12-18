using Domain.Enums;

namespace Domain.Entities
{
    public class Loan : AuditableEntity<Guid>
    {
        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; } = default!;

        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = default!;

        public string LoanNumber { get; set; } = string.Empty;

        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public int TermMonths { get; set; }

        public decimal InstallmentAmount { get; set; }
        public LoanStatus LoanStatus { get; set; }

        public DateTime DisbursementDate { get; set; }
        public DateTime MaturityDate { get; set; }
    }

}
