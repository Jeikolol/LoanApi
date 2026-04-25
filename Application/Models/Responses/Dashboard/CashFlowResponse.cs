namespace Application.Models.Responses.Dashboard
{
    /// <summary>
    /// Cash flow data for the last 4 weeks comparing disbursements vs collections.
    /// Values are in RD$ (Dominican Pesos), whole currency amounts (no decimal places).
    /// </summary>
    public record CashFlowResponse(
        string[] Labels,
        decimal[] Disbursements,
        decimal[] Collections);
}
