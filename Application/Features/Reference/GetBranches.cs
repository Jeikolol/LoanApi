using Dapper;
using MediatR;
using System.Data;

namespace Application.Features.Reference
{
    public class BranchResponse
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>The branches a loan can be booked in.</summary>
    public record GetBranchesQuery : IRequest<IReadOnlyList<BranchResponse>>;

    public class GetBranchesQueryHandler : IRequestHandler<GetBranchesQuery, IReadOnlyList<BranchResponse>>
    {
        private readonly IDbConnection _connection;

        public GetBranchesQueryHandler(IDbConnection connection) => _connection = connection;

        public async Task<IReadOnlyList<BranchResponse>> Handle(GetBranchesQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<BranchResponse> branches = await _connection.QueryAsync<BranchResponse>(
                "SELECT Id, Code, Name FROM [Admin].[Branch] WHERE IsDeleted = 0 ORDER BY Name");

            return branches.ToList();
        }
    }
}
