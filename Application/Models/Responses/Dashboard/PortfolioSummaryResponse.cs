namespace Application.Models.Responses.Dashboard
{
    /// <summary>
    /// Portfolio summary statistics for the dashboard.
    /// All currency values are in RD$ (Dominican Pesos).
    /// </summary>
    public record PortfolioSummaryResponse(
        /// <summary>Principal still owed on loans being repaid, in RD$</summary>
        long TotalPortfolio,
        /// <summary>Delinquency rate as percentage (0-100)</summary>
        decimal DelinquencyRate,
        /// <summary>Count of loans being repaid (disbursed, active, partially paid, defaulted, restructured)</summary>
        int ActiveCredits,
        /// <summary>Total collected this calendar month in RD$</summary>
        long MonthlyCollections);
}
