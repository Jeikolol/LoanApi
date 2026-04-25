using Application.Features.Documents.Commands;
using Application.Exceptions;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Documents.Handlers
{
    public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<DeleteDocumentCommandHandler> _logger;

        public DeleteDocumentCommandHandler(IDbConnection connection, ILogger<DeleteDocumentCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting document {DocumentId}", request.Id);

            const string sql = @"
                UPDATE [Loans].[LoanDocument]
                SET IsDeleted = 1, IsActive = 0, DeletedOn = @Now
                WHERE Id = @Id AND IsDeleted = 0";

            var rowsAffected = await _connection.ExecuteAsync(sql, new
            {
                request.Id,
                Now = DateTime.UtcNow
            });

            if (rowsAffected == 0)
                throw new NotFoundException($"Document {request.Id} not found");
        }
    }
}
