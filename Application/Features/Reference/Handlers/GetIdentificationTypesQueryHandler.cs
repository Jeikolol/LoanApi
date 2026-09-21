using Application.Features.Reference.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using System.Data;

namespace Application.Features.Reference.Handlers
{
    public class GetIdentificationTypesQueryHandler : IRequestHandler<GetIdentificationTypesQuery, IReadOnlyList<IdentificationTypeResponse>>
    {
        private readonly IDbConnection _connection;

        public GetIdentificationTypesQueryHandler(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IReadOnlyList<IdentificationTypeResponse>> Handle(GetIdentificationTypesQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<IdentificationTypeResponse> types = await _connection.QueryAsync<IdentificationTypeResponse>(
                "SELECT Id, Code, Name, Mask FROM [Admin].[IdentificationType] WHERE IsDeleted = 0 ORDER BY Name");

            return types.ToList();
        }
    }
}
