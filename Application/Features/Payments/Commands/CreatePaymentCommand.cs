using Application.Models.Responses;
using Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Payments.Commands
{
    public class CreatePaymentCommand : IRequest<LoanPaymentResponse>
    {
        /// <summary>The employee registering the payment; set from the token, never from the body.</summary>
        [JsonIgnore]
        public Guid? CreatedById { get; set; }

        public Guid LoanId { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PenaltyAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
    }
}
