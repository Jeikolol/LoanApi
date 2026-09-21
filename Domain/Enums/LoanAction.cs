namespace Domain.Enums
{
    /// <summary>The manual steps of a loan's life. Payments and the delinquency calculation move it along by themselves.</summary>
    public enum LoanAction
    {
        Approve = 1,
        Disburse,
        Cancel,
        Close
    }
}
