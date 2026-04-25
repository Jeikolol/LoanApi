using Domain.Enums;

namespace Domain.Entities
{
    public class CreditScore : AuditableEntity<Guid>
    {
        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; } = default!;

        public int Score { get; set; }
        public DateTime LastCalculatedDate { get; set; }

        // Calculation factors
        public decimal PaymentHistoryScore { get; set; }
        public decimal CreditUtilizationScore { get; set; }
        public decimal CreditAgeScore { get; set; }
        public decimal DefaultHistoryScore { get; set; }

        public CreditRating CreditRating { get; set; } = CreditRating.Pending;
    }
}
