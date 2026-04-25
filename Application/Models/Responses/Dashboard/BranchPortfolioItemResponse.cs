namespace Application.Models.Responses.Dashboard
{
    /// <summary>
    /// Portfolio metrics for a single branch.
    /// Currency values are in RD$ (Dominican Pesos).
    /// </summary>
    public record BranchPortfolioItemResponse(
        /// <summary>Branch name</summary>
        string Branch,
        /// <summary>Total principal remaining in RD$</summary>
        long Portfolio,
        /// <summary>Delinquency rate as percentage (0-100)</summary>
        decimal DelinquencyRate,
        /// <summary>Count of loans with days overdue > 30</summary>
        int OpenTickets);
}
