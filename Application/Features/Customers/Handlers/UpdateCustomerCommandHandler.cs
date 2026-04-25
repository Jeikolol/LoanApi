using Application.Features.Customers.Commands;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Customers.Handlers
{
    public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDetailResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<UpdateCustomerCommandHandler> _logger;

        public UpdateCustomerCommandHandler(IDbConnection connection, ILogger<UpdateCustomerCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<CustomerDetailResponse> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating customer {CustomerId}", request.Id);

            var now = DateTime.UtcNow;

            const string updateSql = @"
                UPDATE [Customers].[Customer]
                SET Phone = @Phone, Email = @Email, Address = @Address, Province = @Province,
                    Occupation = @Occupation, AnnualIncome = @AnnualIncome, IncomeSource = @IncomeSource,
                    CreditLimit = @CreditLimit, AlternativePhone = @AlternativePhone,
                    EmergencyContactName = @EmergencyContactName, EmergencyContactPhone = @EmergencyContactPhone,
                    TaxId = @TaxId, UpdatedOn = @Now
                WHERE Id = @Id AND IsDeleted = 0";

            await _connection.ExecuteAsync(updateSql, new
            {
                request.Phone,
                request.Email,
                request.Address,
                request.Province,
                request.Occupation,
                request.AnnualIncome,
                IncomeSource = request.IncomeSource.HasValue ? (int)request.IncomeSource.Value : (int?)null,
                request.CreditLimit,
                request.AlternativePhone,
                request.EmergencyContactName,
                request.EmergencyContactPhone,
                request.TaxId,
                request.Id,
                Now = now
            });

            const string selectSql = @"
                SELECT c.Id, c.CustomerType, c.FirstName, c.LastName, c.CompanyName, c.Identification, c.IdentificationTypeId,
                       c.Phone, c.Email, c.Address, c.Province, c.CountryId, c.Status, c.Occupation, c.AnnualIncome,
                       c.IncomeSource, c.CreditLimit, c.RiskLevel, c.IsBlacklisted, c.BlacklistReason, c.TaxId,
                       c.AlternativePhone, c.EmergencyContactName, c.EmergencyContactPhone, c.CreatedOn, c.UpdatedOn
                FROM [Customers].[Customer] c
                WHERE c.Id = @Id AND c.IsDeleted = 0";

            var customer = await _connection.QuerySingleOrDefaultAsync<CustomerDetailResponse>(
                selectSql, new { request.Id });

            return customer ?? throw new InvalidOperationException("Customer not found");
        }
    }
}
