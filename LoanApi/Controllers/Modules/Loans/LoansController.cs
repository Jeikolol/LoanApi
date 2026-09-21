using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using LoanApi.Controllers.Modules.Loans.Requests;
using LoanApi.Common.Pagination;
using LoanApi.Common.Extensions;
using Application.Features.Loans.Queries;
using Application.Features.Loans.Commands;
using Application.Features.Payments.Commands;
using Application.Features.Documents.Commands;
using Application.Features.Delinquency.Queries;
using Domain.Enums;

namespace LoanApi.Controllers.Modules.Loans
{
    [Authorize]
    public class LoansController : LoanModuleController
    {
        /// <summary>
        /// Create a new loan
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLoanRequest request)
        {
            var command = new CreateLoanCommand
            {
                CustomerId = request.CustomerId,
                BranchId = request.BranchId,
                PrincipalAmount = request.PrincipalAmount,
                InterestRate = request.InterestRate,
                TermMonths = request.TermMonths
            };

            if (Guid.TryParse(User.FindFirst("sub")?.Value, out Guid userId))
            {
                command.CreatedById = userId;
            }

            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Get all loans (paginated)
        /// </summary>
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetLoansQuery { PageNumber = pageNumber, PageSize = pageSize };
            var result = await Mediator.Send(query);
            return this.OkPaged(result.Data, new PageRequest(pageNumber, pageSize), result.TotalCount);
        }

        /// <summary>
        /// Get loan by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var query = new GetLoanByIdQuery(id);
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get customer's loans (paginated)
        /// </summary>
        [HttpPost("GetByCustomer")]
        public async Task<IActionResult> GetByCustomer([FromBody] GetLoansByCustomerRequest request)
        {
            var query = new GetLoansByCustomerQuery(request.CustomerId, request.PageNumber, request.PageSize);
            var result = await Mediator.Send(query);
            return this.OkPaged(result.Data, new PageRequest(request.PageNumber, request.PageSize), result.TotalCount);
        }

        /// <summary>
        /// Update loan
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = SystemRoles.AdminOrManager)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLoanRequest request)
        {
            var command = new UpdateLoanCommand
            {
                Id = id,
                InterestRate = request.InterestRate,
                LoanStatus = request.LoanStatus
            };
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Approve a pending loan (checks the customer's eligibility and credit limit)
        /// </summary>
        [HttpPost("Approve/{id:guid}")]
        [Authorize(Roles = SystemRoles.AdminOrManager)]
        public Task<IActionResult> Approve(Guid id) => Transition(id, LoanAction.Approve);

        /// <summary>
        /// Disburse an approved loan: starts its schedule today
        /// </summary>
        [HttpPost("Disburse/{id:guid}")]
        [Authorize(Roles = SystemRoles.AdminOrManager)]
        public Task<IActionResult> Disburse(Guid id) => Transition(id, LoanAction.Disburse);

        /// <summary>
        /// Cancel a loan that has not been disbursed
        /// </summary>
        [HttpPost("Cancel/{id:guid}")]
        [Authorize(Roles = SystemRoles.AdminOrManager)]
        public Task<IActionResult> Cancel(Guid id) => Transition(id, LoanAction.Cancel);

        /// <summary>
        /// Close a fully paid loan
        /// </summary>
        [HttpPost("Close/{id:guid}")]
        [Authorize(Roles = SystemRoles.AdminOrManager)]
        public Task<IActionResult> Close(Guid id) => Transition(id, LoanAction.Close);

        private async Task<IActionResult> Transition(Guid id, LoanAction action)
        {
            var result = await Mediator.Send(new TransitionLoanCommand { LoanId = id, Action = action });
            return Ok(result);
        }

        /// <summary>
        /// Get amortization schedule
        /// </summary>
        [HttpGet("GetAmortization/{id}")]
        public async Task<IActionResult> GetAmortizationSchedule(Guid id)
        {
            var query = new GetAmortizationScheduleQuery(id);
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get loan documents (paginated)
        /// </summary>
        [HttpGet("GetDocuments/{id}")]
        public async Task<IActionResult> GetDocuments(Guid id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetLoanDocumentsQuery(id, pageNumber, pageSize);
            var result = await Mediator.Send(query);
            return this.OkPaged(result.Data, new PageRequest(pageNumber, pageSize), result.TotalCount);
        }

        /// <summary>
        /// Upload loan document
        /// </summary>
        [HttpPost("CreateDocument/{id}")]
        public async Task<IActionResult> CreateDocument(Guid id, [FromBody] UploadDocumentRequest request)
        {
            var command = new CreateDocumentCommand
            {
                LoanId = id,
                DocumentType = ParseDocumentType(request.DocumentType),
                FileName = request.FileName ?? string.Empty,
                FileUrl = request.FileUrl ?? string.Empty
            };
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Get all delinquent loans (paginated)
        /// </summary>
        [HttpGet("delinquent/list")]
        public async Task<IActionResult> GetDelinquent([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetDelinquentLoansQuery { PageNumber = pageNumber, PageSize = pageSize };
            var result = await Mediator.Send(query);
            return this.OkPaged(result.Data, new PageRequest(pageNumber, pageSize), result.TotalCount);
        }

        /// <summary>
        /// Get dashboard summary
        /// </summary>
        [HttpGet("dashboard/summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var query = new GetDashboardSummaryQuery();
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        private static DocumentTypes ParseDocumentType(string? value)
        {
            if (!Application.Features.Documents.DocumentRules.TryParseDocumentType(value, out var type))
                throw new Application.Exceptions.ValidationException(
                    "The document is not valid.", new List<string> { "The document type is not recognized." });
            return type;
        }
    }
}
