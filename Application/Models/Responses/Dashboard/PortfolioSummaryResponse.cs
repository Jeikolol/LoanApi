namespace Application.Models.Responses.Dashboard
{
    /// <summary>
    /// Portfolio summary statistics for the dashboard.
    /// All currency values are in RD$ (Dominican Pesos).
    /// </summary>
    public record PortfolioSummaryResponse(
        /// <summary>Total principal amount in RD$</summary>
        long TotalPortfolio,
        /// <summary>Delinquency rate as percentage (0-100)</summary>
        decimal DelinquencyRate,
        /// <summary>Count of active loans (status = 3)</summary>
        int ActiveCredits,
        /// <summary>Total collections for today in RD$</summary>
        long MonthlyCollections);
}
