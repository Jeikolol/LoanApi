namespace Domain.Entities
{
    public class Currency : AuditableEntity<Guid>
    {
        public string CodeIso2 { get; set; } = string.Empty;
        public string CodeIso3 { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;

        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
        public ICollection<LoanPayment> LoanPayments { get; set; } = new List<LoanPayment>();
    }
}
