namespace Application.Models.Responses
{
    /// <summary>What a late fee charge did: how many overdue days it covered and what it added.</summary>
    public record LateFeeChargeResponse(
        Guid LoanId,
        string LoanNumber,
        int DaysCharged,
        decimal AmountCharged,
        decimal TotalLateFees,
        DateOnly ChargedThrough
    );
}
