using MediatR;

namespace Application.Features.Documents.Commands
{
    public record DeleteDocumentCommand(Guid Id) : IRequest;
}
