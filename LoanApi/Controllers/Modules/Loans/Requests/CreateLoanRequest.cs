namespace LoanApi.Controllers.Modules.Loans.Requests
{
    public class CreateLoanRequest
    {
        public Guid CustomerId { get; set; }
        public Guid BranchId { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public int TermMonths { get; set; }
    }
}
