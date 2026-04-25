namespace LoanApi.Controllers.Modules.Loans.Requests
{
    public class GetLoansByCustomerRequest
    {
        public Guid CustomerId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
