namespace LoanApi.Controllers.Modules.Loans.Requests
{
    public class UpdateLoanRequest
    {
        public decimal? InterestRate { get; set; }
        public string? LoanStatus { get; set; }
    }
}
