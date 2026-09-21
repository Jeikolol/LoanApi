using Application.Features.Reference.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using System.Data;

namespace Application.Features.Reference.Handlers
{
    public class GetCountriesQueryHandler : IRequestHandler<GetCountriesQuery, IReadOnlyList<CountryResponse>>
    {
        private readonly IDbConnection _connection;

        public GetCountriesQueryHandler(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IReadOnlyList<CountryResponse>> Handle(GetCountriesQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<CountryResponse> countries = await _connection.QueryAsync<CountryResponse>(
                "SELECT Id, CodeIso2, Name, Nationality, PhoneCode, PhoneMask FROM [Reference].[Country] WHERE IsDeleted = 0 ORDER BY Name");

            return countries.ToList();
        }
    }
}
