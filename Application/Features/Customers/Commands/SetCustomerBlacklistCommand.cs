using Application.Models.Responses;
using MediatR;

namespace Application.Features.Customers.Commands
{
    /// <summary>Puts a customer on the blacklist (with the reason) or takes them off it.</summary>
    public class SetCustomerBlacklistCommand : IRequest<CustomerDetailResponse>
    {
        public Guid Id { get; set; }
        public bool Blacklisted { get; set; }
        public string? Reason { get; set; }
    }
}
