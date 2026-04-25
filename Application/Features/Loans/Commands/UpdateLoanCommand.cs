using Application.Models.Responses;
using MediatR;

namespace Application.Features.Loans.Commands
{
    public class UpdateLoanCommand : IRequest<LoanDetailResponse>
    {
        public Guid Id { get; set; }
        public decimal? InterestRate { get; set; }
        public string? LoanStatus { get; set; }
    }
}
