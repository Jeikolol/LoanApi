using Application.Exceptions;
using Application.Features.Customers.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Customers.Handlers
{
    public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDetailResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetCustomerByIdQueryHandler> _logger;

        public GetCustomerByIdQueryHandler(IDbConnection connection, ILogger<GetCustomerByIdQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<CustomerDetailResponse> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting customer by ID: {CustomerId}", request.Id);

            const string query = @"
                SELECT C.Id, C.CustomerType, C.FirstName, C.LastName, C.CompanyName,
                       C.Identification, C.IdentificationTypeId, C.Phone, C.Email,
                       C.Address, C.Province, C.CountryId, C.Status, C.Occupation,
                       C.AnnualIncome, C.IncomeSource, C.CreditLimit, C.RiskLevel,
                       C.IsBlacklisted, C.BlacklistReason, C.TaxId,
                       C.AlternativePhone, C.EmergencyContactName, C.EmergencyContactPhone,
                       C.CreatedOn, C.UpdatedOn
                FROM [Customers].[Customer] C
                WHERE C.Id = @Id AND C.IsDeleted = 0";

            var customer = await _connection.QueryFirstOrDefaultAsync<CustomerDetailResponse>(query, new { request.Id });
            return customer ?? throw new NotFoundException($"Customer {request.Id} not found");
        }
    }
}
