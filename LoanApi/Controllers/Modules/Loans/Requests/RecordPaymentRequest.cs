namespace LoanApi.Controllers.Modules.Loans.Requests
{
    public class RecordPaymentRequest
    {
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PenaltyAmount { get; set; }
        public string PaymentMethod { get; set; } = "Bank Transfer";
        public string? ReferenceNumber { get; set; }
    }
}
