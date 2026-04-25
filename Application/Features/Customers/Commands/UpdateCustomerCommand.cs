using Application.Models.Responses;
using Domain.Enums;
using MediatR;

namespace Application.Features.Customers.Commands
{
    public class UpdateCustomerCommand : IRequest<CustomerDetailResponse>
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Province { get; set; }
        public string? Occupation { get; set; }
        public decimal? AnnualIncome { get; set; }
        public IncomeSource? IncomeSource { get; set; }
        public decimal CreditLimit { get; set; }
        public string? AlternativePhone { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? TaxId { get; set; }
    }
}
