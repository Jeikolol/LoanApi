using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
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
        /// Get payment history (paginated)
        /// </summary>
        [HttpGet("GetPayments/{id}")]
        public async Task<IActionResult> GetPayments(Guid id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetLoanPaymentsQuery(id, pageNumber, pageSize);
            var result = await Mediator.Send(query);
            return this.OkPaged(result.Data, new PageRequest(pageNumber, pageSize), result.TotalCount);
        }

        /// <summary>
        /// Record payment
        /// </summary>
        [HttpPost("CreatePayment/{id}")]
        public async Task<IActionResult> CreatePayment(Guid id, [FromBody] RecordPaymentRequest request)
        {
            var command = new CreatePaymentCommand
            {
                LoanId = id,
                PrincipalAmount = request.PrincipalAmount,
                InterestAmount = request.InterestAmount,
                PenaltyAmount = request.PenaltyAmount,
                PaymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod, ignoreCase: true),
                ReferenceNumber = request.ReferenceNumber
            };
            var result = await Mediator.Send(command);
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
                DocumentType = Enum.Parse<DocumentTypes>(request.DocumentType ?? "Contract", ignoreCase: true),
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
    }
}
