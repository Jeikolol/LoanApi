namespace Application.Models.Responses.Dashboard
{
    /// <summary>
    /// Delinquency trend data for the last 30 days.
    /// Compares current period average days overdue vs previous 30-day rolling window.
    /// </summary>
    public record DelinquencyTrendResponse(
        /// <summary>Date labels in dd/MM format for the X-axis</summary>
        string[] Labels,
        /// <summary>Current period rolling average days overdue</summary>
        decimal[] CurrentMonth,
        /// <summary>Previous 30-day rolling window average days overdue</summary>
        decimal[] PreviousMonth);
}
