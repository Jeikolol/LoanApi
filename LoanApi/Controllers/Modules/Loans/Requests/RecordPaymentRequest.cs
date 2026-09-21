using Domain.Enums;

namespace LoanApi.Controllers.Modules.Loans.Requests
{
    public class RecordPaymentRequest
    {
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PenaltyAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;
        public string? ReferenceNumber { get; set; }
    }
}
