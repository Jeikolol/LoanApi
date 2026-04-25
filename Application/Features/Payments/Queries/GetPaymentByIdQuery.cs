using Application.Models.Responses;
using MediatR;

namespace Application.Features.Payments.Queries
{
    public record GetPaymentByIdQuery(Guid Id) : IRequest<LoanPaymentResponse>;
}
