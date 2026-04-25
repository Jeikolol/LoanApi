using Application.Services;
using MediatR;

namespace Application.Features.Loans.Queries
{
    public record GetAmortizationScheduleQuery(Guid LoanId) : IRequest<List<AmortizationScheduleItem>>;
}
