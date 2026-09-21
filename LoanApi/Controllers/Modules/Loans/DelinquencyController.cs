using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using LoanApi.Common.Pagination;
using LoanApi.Common.Extensions;
using Application.Features.Delinquency.Queries;
using Application.Features.Delinquency.Commands;

namespace LoanApi.Controllers.Modules.Loans
{
    [Authorize]
    [Route("api/v{version:apiVersion}/delinquency")]
    public class DelinquencyController : LoanModuleController
    {
        /// <summary>
        /// Get all delinquent loans (paginated)
        /// </summary>
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetDelinquentLoansQuery { PageNumber = pageNumber, PageSize = pageSize };
            var result = await Mediator.Send(query);
            return this.OkPaged(result.Data, new PageRequest(pageNumber, pageSize), result.TotalCount);
        }

        /// <summary>
        /// Get loan delinquency status
        /// </summary>
        [HttpGet("{loanId:guid}")]
        public async Task<IActionResult> Get([FromRoute] Guid loanId)
        {
            var query = new GetDelinquencyStatusQuery(loanId);
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Calculate delinquency for a loan
        /// </summary>
        [HttpPost("{loanId:guid}/calculate")]
        public async Task<IActionResult> CalculateDelinquency([FromRoute] Guid loanId)
        {
            var command = new CalculateDelinquencyCommand(loanId);
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Bring the days overdue, percentage and status of every loan being repaid up to date (moves no money)
        /// </summary>
        [HttpPost("recalculate-all")]
        [Authorize(Roles = SystemRoles.AdminOrManager)]
        public async Task<IActionResult> RecalculateAll()
        {
            var result = await Mediator.Send(new Application.Features.Delinquency.RefreshDelinquencyCommand());
            return Ok(result);
        }

        /// <summary>
        /// Charge the late fees of every overdue loan (each overdue day is charged once, so it is safe to repeat)
        /// </summary>
        [HttpPost("charge-fees")]
        [Authorize(Roles = SystemRoles.Admin)]
        public async Task<IActionResult> ChargeAllLateFees()
        {
            var result = await Mediator.Send(new Application.Features.Delinquency.ChargeAllLateFeesCommand());
            return Ok(result);
        }

        /// <summary>
        /// Write off a loan that is past the policy's write-off period (administrators only; the reason is kept)
        /// </summary>
        [HttpPost("{loanId:guid}/write-off")]
        [Authorize(Roles = SystemRoles.Admin)]
        public async Task<IActionResult> WriteOff([FromRoute] Guid loanId, [FromBody] WriteOffLoanRequest request)
        {
            var result = await Mediator.Send(new Application.Features.Delinquency.Commands.WriteOffLoanCommand { LoanId = loanId, Reason = request.Reason ?? string.Empty });
            return Ok(result);
        }

        /// <summary>
        /// Charge late fees for a delinquent loan
        /// </summary>
        [HttpPost("{loanId:guid}/charge-fees")]
        [Authorize(Roles = SystemRoles.Admin)]
        public async Task<IActionResult> ChargeLateFees([FromRoute] Guid loanId)
        {
            var command = new ChargeLateFeeCommand(loanId);
            var result = await Mediator.Send(command);
            return Ok(result);
        }
    }

    public class WriteOffLoanRequest
    {
        public string? Reason { get; set; }
    }
}
