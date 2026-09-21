using Dapper;
using System.Data;

namespace LoanApi.Test.TestHelpers;

public sealed class SqliteGuidHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value) => Guid.Parse((string)value);

    public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value.ToString();
}

public sealed class SqliteDateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => DateOnly.Parse((string)value);

    public override void SetValue(IDbDataParameter parameter, DateOnly value) => parameter.Value = value.ToString("yyyy-MM-dd");
}
