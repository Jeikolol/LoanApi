using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoanApi.Common.Pagination;
using LoanApi.Common.Extensions;
using Application.Features.Payments.Queries;
using Application.Features.Payments.Commands;
using LoanApi.Controllers.Modules.Loans.Requests;
using Domain.Constants;

namespace LoanApi.Controllers.Modules.Loans
{
    [Authorize]
    [Route("api/v{version:apiVersion}/payments")]
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
        public async Task<IActionResult> Create(Guid loanId, [FromBody] RecordPaymentRequest request)
        {
            var command = new CreatePaymentCommand
            {
                LoanId = loanId,
                PrincipalAmount = request.PrincipalAmount,
                InterestAmount = request.InterestAmount,
                PenaltyAmount = request.PenaltyAmount,
                PaymentMethod = request.PaymentMethod,
                ReferenceNumber = request.ReferenceNumber,
                CreatedById = Guid.TryParse(User.FindFirst("sub")?.Value, out Guid employeeId) ? employeeId : null,
            };
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Reverse a completed payment. It stays in the history as Reversed with its reason, and the loan goes back
        /// to what it was without it.
        /// </summary>
        [HttpPost("Reverse/{id:guid}")]
        [Authorize(Roles = SystemRoles.AdminOrManager)]
        public async Task<IActionResult> Reverse(Guid id, [FromBody] ReversePaymentRequest request)
        {
            var result = await Mediator.Send(new ReversePaymentCommand { Id = id, Reason = request.Reason ?? string.Empty });
            return Ok(result);
        }
    }
}
