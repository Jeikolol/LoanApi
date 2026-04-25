using Application.Models.Responses;
using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Commands
{
    public class CreateLoanCommand : IRequest<LoanDetailResponse>
    {
        public Guid CustomerId { get; set; }
        public Guid BranchId { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public int TermMonths { get; set; }
        public LoanStatus LoanStatus { get; set; } = LoanStatus.Pending;
    }
}
