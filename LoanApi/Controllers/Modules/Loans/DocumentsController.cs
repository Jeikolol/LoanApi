using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoanApi.Controllers.Modules.Loans.Requests;
using LoanApi.Common.Pagination;
using LoanApi.Common.Extensions;
using Application.Features.Documents.Queries;
using Application.Features.Documents.Commands;
using Domain.Enums;

namespace LoanApi.Controllers.Modules.Loans
{
    [Authorize]
    [Route("documents")]
    public class DocumentsController : LoanModuleController
    {
        /// <summary>
        /// Get document by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var query = new GetDocumentByIdQuery(id);
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get all documents for a loan (paginated)
        /// </summary>
        [HttpGet("GetByLoan/{loanId:guid}")]
        public async Task<IActionResult> GetAll(Guid loanId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetDocumentsByLoanQuery(loanId, pageNumber, pageSize);
            var result = await Mediator.Send(query);
            return this.OkPaged(result.Data, new PageRequest(pageNumber, pageSize), result.TotalCount);
        }

        /// <summary>
        /// Upload document for a loan
        /// </summary>
        [HttpPost("CreateForLoan/{loanId:guid}")]
        public async Task<IActionResult> Create(Guid loanId, [FromBody] UploadDocumentRequest request)
        {
            var command = new CreateDocumentCommand
            {
                LoanId = loanId,
                DocumentType = Enum.Parse<DocumentTypes>(request.DocumentType ?? "Contract", ignoreCase: true),
                FileName = request.FileName ?? string.Empty,
                FileUrl = request.FileUrl ?? string.Empty
            };
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Delete document
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteDocumentCommand(id);
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
