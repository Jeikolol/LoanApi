using Dapper;
using System.Data;

namespace Application.Features.Employees
{
    public interface IEmployeeAccessValidator
    {
        /// <summary>Whether a token that was validly signed should still be accepted for this employee.</summary>
        Task<bool> IsTokenStillValidAsync(Guid employeeId, long? issuedAtUnixSeconds, CancellationToken cancellationToken = default);
    }

    public sealed class EmployeeAccessValidator : IEmployeeAccessValidator
    {
        private readonly IDbConnection _connection;

        public EmployeeAccessValidator(IDbConnection connection) => _connection = connection;

        public async Task<bool> IsTokenStillValidAsync(Guid employeeId, long? issuedAtUnixSeconds, CancellationToken cancellationToken = default)
        {
            AccessRow? row = await _connection.QueryFirstOrDefaultAsync<AccessRow>(
                "SELECT IsActive, IsDeleted, TokensValidFrom FROM [Admin].[Employee] WHERE Id = @Id",
                new { Id = employeeId });

            return row is not null && EmployeeRules.IsTokenValid(row.IsActive, row.IsDeleted, row.TokensValidFrom, issuedAtUnixSeconds);
        }

        private sealed class AccessRow
        {
            public bool IsActive { get; set; }
            public bool IsDeleted { get; set; }
            public DateTime? TokensValidFrom { get; set; }
        }
    }
}
