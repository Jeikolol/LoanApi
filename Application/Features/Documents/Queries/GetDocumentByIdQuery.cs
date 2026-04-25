using Application.Models.Responses;
using MediatR;

namespace Application.Features.Documents.Queries
{
    public record GetDocumentByIdQuery(Guid Id) : IRequest<LoanDocumentResponse>;
}
