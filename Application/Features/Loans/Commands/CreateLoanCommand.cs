using Application.Models.Responses;
using Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Loans.Commands
{
    public class CreateLoanCommand : IRequest<LoanDetailResponse>
    {
        [JsonIgnore]
        public Guid? CreatedById { get; set; }

        public Guid CustomerId { get; set; }
        public Guid BranchId { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public int TermMonths { get; set; }
    }
}
