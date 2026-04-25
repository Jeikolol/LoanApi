using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoanApi.Common.Pagination;
using LoanApi.Common.Extensions;
using Application.Features.Payments.Queries;
using Application.Features.Payments.Commands;

namespace LoanApi.Controllers.Modules.Loans
{
    [Authorize]
    [Route("payments")]
    public class PaymentsController : LoanModuleController
    {
        /// <summary>
        /// Get payment by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var query = new GetPaymentByIdQuery(id);
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get all payments for a loan (paginated)
        /// </summary>
        [HttpGet("loan/{loanId:guid}")]
        public async Task<IActionResult> GetAll(Guid loanId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetPaymentsByLoanQuery(loanId, pageNumber, pageSize);
            var result = await Mediator.Send(query);
            return this.OkPaged(result.Data, new PageRequest(pageNumber, pageSize), result.TotalCount);
        }

        /// <summary>
        /// Record a payment for a loan
        /// </summary>
        [HttpPost("loan/{loanId:guid}")]
        public async Task<IActionResult> Create(Guid loanId, [FromBody] dynamic request)
        {
            var command = new CreatePaymentCommand
            {
                LoanId = loanId,
                PrincipalAmount = request.PrincipalAmount ?? 0,
                InterestAmount = request.InterestAmount ?? 0,
                PenaltyAmount = request.PenaltyAmount ?? 0,
                PaymentMethod = request.PaymentMethod ?? string.Empty,
                ReferenceNumber = request.ReferenceNumber
            };
            var result = await Mediator.Send(command);
            return Ok(result);
        }
    }
}
