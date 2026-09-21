using Domain.Enums;

namespace LoanApi.Controllers.Modules.Customers.Requests
{
    public class UpdateCustomerRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? CompanyName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AlternativePhone { get; set; }
        public string? Address { get; set; }
        public string? Province { get; set; }
        public string? Occupation { get; set; }
        public decimal? AnnualIncome { get; set; }
        public IncomeSource? IncomeSource { get; set; }
        public decimal CreditLimit { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? TaxId { get; set; }
    }
}
