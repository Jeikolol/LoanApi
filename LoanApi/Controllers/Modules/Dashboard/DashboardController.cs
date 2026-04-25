using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Models.Responses.Dashboard;
using Application.Features.Dashboard.Queries;

namespace LoanApi.Controllers.Modules.Dashboard;

[Authorize]
public class DashboardController : BaseApiController
{
    /// <summary>
    /// Get portfolio summary statistics (total portfolio, delinquency rate, active loans, collections)
    /// </summary>
    [HttpGet("portfolio-summary")]
    public async Task<IActionResult> GetPortfolioSummary()
    {
        var result = await Mediator.Send(new GetPortfolioSummaryQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get loan portfolio status breakdown by loan status
    /// </summary>
    [HttpGet("portfolio-status")]
    public async Task<IActionResult> GetPortfolioStatus()
    {
        var result = await Mediator.Send(new GetPortfolioStatusQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get delinquency trend for last 30 days
    /// </summary>
    [HttpGet("delinquency-trend")]
    public async Task<IActionResult> GetDelinquencyTrend()
    {
        var result = await Mediator.Send(new GetDelinquencyTrendQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get cash flow data (last 4 weeks: disbursements vs collections)
    /// </summary>
    [HttpGet("cash-flow")]
    public async Task<IActionResult> GetCashFlow()
    {
        var result = await Mediator.Send(new GetCashFlowQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get portfolio metrics by branch
    /// </summary>
    [HttpGet("branch-portfolio")]
    public async Task<IActionResult> GetBranchPortfolio()
    {
        var result = await Mediator.Send(new GetBranchPortfolioQuery());
        return Ok(result);
    }
}
