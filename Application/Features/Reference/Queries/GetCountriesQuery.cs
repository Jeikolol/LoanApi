using Application.Models.Responses;
using MediatR;

namespace Application.Features.Reference.Queries
{
    public record GetCountriesQuery : IRequest<IReadOnlyList<CountryResponse>>;
}
