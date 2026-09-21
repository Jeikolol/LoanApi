using Application.Models.Responses;
using MediatR;

namespace Application.Features.Reference.Queries
{
    public record GetIdentificationTypesQuery : IRequest<IReadOnlyList<IdentificationTypeResponse>>;
}
