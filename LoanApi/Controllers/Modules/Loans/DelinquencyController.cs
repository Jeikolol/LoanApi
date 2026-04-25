using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoanApi.Common.Pagination;
using LoanApi.Common.Extensions;
using Application.Features.Delinquency.Queries;
using Application.Features.Delinquency.Commands;

namespace LoanApi.Controllers.Modules.Loans
{
    [Authorize]
    [Route("delinquency")]
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
        /// Charge late fees for a delinquent loan
        /// </summary>
        [HttpPost("{loanId:guid}/charge-fees")]
        public async Task<IActionResult> ChargeLateFees([FromRoute] Guid loanId)
        {
            var command = new ChargeLateFeeCommand(loanId);
            var result = await Mediator.Send(command);
            return Ok(result);
        }
    }
}
