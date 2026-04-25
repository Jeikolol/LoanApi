using Dapper;
using System.Data;

namespace LoanApi.Tests.TestHelpers;

public class DapperConnectionAdapter : IDbConnectionWrapper
{
    private readonly IDbConnection _connection;

    public DapperConnectionAdapter(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<T> QuerySingleOrDefaultAsync<T>(string sql, object? param = null)
    {
        return await _connection.QuerySingleOrDefaultAsync<T>(sql, param);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        return await _connection.ExecuteAsync(sql, param);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        return await _connection.QueryAsync<T>(sql, param);
    }
}

public interface IDbConnectionWrapper
{
    Task<T> QuerySingleOrDefaultAsync<T>(string sql, object? param = null);
    Task<int> ExecuteAsync(string sql, object? param = null);
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);
}
