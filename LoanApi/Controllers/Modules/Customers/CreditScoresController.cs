using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Features.CreditScores.Queries;
using Application.Features.CreditScores.Commands;

namespace LoanApi.Controllers.Modules.Customers
{
    [Authorize]
    [Route("credit-scores")]
    public class CreditScoresController : CustomerModuleController
    {
        /// <summary>
        /// Get customer credit score
        /// </summary>
        [HttpGet("{customerId:guid}")]
        public async Task<IActionResult> Get(Guid customerId)
        {
            var query = new GetCreditScoreQuery(customerId);
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Recalculate customer credit score
        /// </summary>
        [HttpPost("{customerId}/recalculate")]
        public async Task<IActionResult> Recalculate(Guid customerId)
        {
            var command = new RecalculateCreditScoreCommand(customerId);
            var result = await Mediator.Send(command);
            return Ok(result);
        }
    }
}
