using Application.Models.Responses;
using MediatR;

namespace Application.Features.Customers.Queries
{
    public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDetailResponse>;
}
