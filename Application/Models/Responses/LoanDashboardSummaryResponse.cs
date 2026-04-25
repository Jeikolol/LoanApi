namespace Application.Models.Responses
{
    public record LoanDashboardSummaryResponse(
        int TotalLoans,
        int TotalActiveLoans,
        decimal TotalPrincipalDisbursed,
        int TotalDelinquentLoans,
        decimal TotalPrincipalRemaining
    );
}
