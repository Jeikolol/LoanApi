using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using LoanApi.Controllers.Modules.Loans.Requests;
using LoanApi.Common.Pagination;
using LoanApi.Common.Extensions;
using Application.Features.Documents.Queries;
using Application.Features.Documents.Commands;
using Domain.Enums;
using Application.Features.Documents;

namespace LoanApi.Controllers.Modules.Loans
{
    [Authorize]
    [Route("api/v{version:apiVersion}/documents")]
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
                DocumentType = ParseDocumentType(request.DocumentType),
                FileName = request.FileName ?? string.Empty,
                FileUrl = request.FileUrl ?? string.Empty
            };
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Upload a file for a loan. It is kept in Azure when that is enabled, otherwise in local storage.
        /// </summary>
        [HttpPost("Upload/{loanId:guid}")]
        [RequestSizeLimit(DocumentRules.MaxFileBytes + 1024 * 1024)]
        public async Task<IActionResult> Upload(Guid loanId, [FromForm] UploadDocumentForm form)
        {
            if (form.File is null)
                throw new Application.Exceptions.ValidationException(
                    "The document is not valid.", new List<string> { "A file is required." });

            if (!DocumentRules.TryParseDocumentType(form.DocumentType, out var documentType))
                throw new Application.Exceptions.ValidationException(
                    "The document is not valid.", new List<string> { "The document type is not recognized." });

            await using var content = form.File.OpenReadStream();
            var command = new UploadDocumentCommand
            {
                LoanId = loanId,
                DocumentType = documentType,
                // Browsers send just the name, but some clients send a full path.
                FileName = Path.GetFileName(form.File.FileName),
                Length = form.File.Length,
                Content = content
            };

            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Download the file behind a document. Only signed-in users can, and it is always sent as an attachment.
        /// </summary>
        [HttpGet("Download/{id:guid}")]
        public async Task<IActionResult> Download(Guid id)
        {
            var file = await Mediator.Send(new GetDocumentFileQuery(id));
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Delete document
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = SystemRoles.Admin)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteDocumentCommand(id);
            await Mediator.Send(command);
            return NoContent();
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
