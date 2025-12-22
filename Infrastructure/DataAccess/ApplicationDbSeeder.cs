using Persistence;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.DataAccess
{
    public class ApplicationDbSeeder
    {
        //En caso de querer insertar muchos datos; usar Dapper para mejor performance
        private readonly IDbConnection _connection;

        public ApplicationDbSeeder(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task SeedDatabaseAsync(LoanDbContext dbContext, CancellationToken cancellationToken)
        {
        }
    }
}
