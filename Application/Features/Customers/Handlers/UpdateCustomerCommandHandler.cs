using Application.Exceptions;
using Application.Features.Customers.Commands;
using Application.Models.Responses;
using Dapper;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

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

            Normalize(request);

            int? typeValue = await _connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT CustomerType FROM [Customers].[Customer] WHERE Id = @Id AND IsDeleted = 0", new { request.Id });
            if (typeValue is null) throw new NotFoundException($"Customer {request.Id} not found");

            Validate(request, (CustomerType)typeValue.Value);
            await EnsurePhoneNotTaken(request);

            try
            {
                await _connection.ExecuteAsync(
                    @"UPDATE [Customers].[Customer]
                      SET FirstName = @FirstName, LastName = @LastName, CompanyName = @CompanyName,
                          Phone = @Phone, AlternativePhone = @AlternativePhone, Email = @Email,
                          Address = @Address, Province = @Province, Occupation = @Occupation,
                          AnnualIncome = @AnnualIncome, IncomeSource = @IncomeSource, CreditLimit = @CreditLimit,
                          EmergencyContactName = @EmergencyContactName, EmergencyContactPhone = @EmergencyContactPhone,
                          TaxId = @TaxId, UpdatedOn = @Now
                      WHERE Id = @Id AND IsDeleted = 0",
                    new
                    {
                        request.Id,
                        request.FirstName,
                        request.LastName,
                        request.CompanyName,
                        request.Phone,
                        request.AlternativePhone,
                        request.Email,
                        request.Address,
                        request.Province,
                        Occupation = request.Occupation ?? string.Empty,
                        request.AnnualIncome,
                        IncomeSource = (int?)request.IncomeSource,
                        request.CreditLimit,
                        request.EmergencyContactName,
                        request.EmergencyContactPhone,
                        request.TaxId,
                        Now = DateTime.UtcNow,
                    });
            }
            catch (DbException ex) when (ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                // Two requests passed the duplicate check at the same time; the unique index decides.
                throw new ConflictException("A customer with this phone number already exists.");
            }

            const string selectSql = @"
                SELECT c.Id, c.CustomerType, c.FirstName, c.LastName, c.CompanyName, c.Identification, c.IdentificationTypeId,
                       c.Phone, c.Email, c.Address, c.Province, c.CountryId, c.Status, c.Occupation, c.AnnualIncome,
                       c.IncomeSource, c.CreditLimit, c.RiskLevel, c.IsBlacklisted, c.BlacklistReason, c.TaxId,
                       c.AlternativePhone, c.EmergencyContactName, c.EmergencyContactPhone, c.CreatedOn, c.UpdatedOn
                FROM [Customers].[Customer] c
                WHERE c.Id = @Id AND c.IsDeleted = 0";

            CustomerDetailResponse? customer = await _connection.QuerySingleOrDefaultAsync<CustomerDetailResponse>(
                selectSql, new { request.Id });

            return customer ?? throw new NotFoundException($"Customer {request.Id} not found");
        }

        private static void Normalize(UpdateCustomerCommand request)
        {
            request.FirstName = request.FirstName.Trim();
            request.LastName = request.LastName.Trim();
            request.Phone = PhoneRules.Normalize(request.Phone) ?? string.Empty;
            request.Address = request.Address.Trim();
            request.Province = request.Province.Trim();
            request.CompanyName = CustomerFieldRules.Clean(request.CompanyName);
            request.TaxId = CustomerFieldRules.Clean(request.TaxId);
            request.AlternativePhone = PhoneRules.Normalize(request.AlternativePhone);
            request.Email = CustomerFieldRules.Clean(request.Email);
            request.Occupation = CustomerFieldRules.Clean(request.Occupation);
            request.EmergencyContactName = CustomerFieldRules.Clean(request.EmergencyContactName);
            request.EmergencyContactPhone = PhoneRules.Normalize(request.EmergencyContactPhone);
        }

        private static void Validate(UpdateCustomerCommand request, CustomerType type)
        {
            List<string> errors = [];

            CustomerFieldRules.Required(errors, "FirstName", request.FirstName, 100);
            CustomerFieldRules.Required(errors, "LastName", request.LastName, 100);
            CustomerFieldRules.Required(errors, "Address", request.Address, 250);
            CustomerFieldRules.Required(errors, "Province", request.Province, 100);

            if (type == CustomerType.LegalPerson && request.CompanyName is null) errors.Add("CompanyName is required for a legal person.");
            if (request.CompanyName?.Length > 200) errors.Add("CompanyName cannot exceed 200 characters.");
            if (request.TaxId?.Length > 50) errors.Add("TaxId cannot exceed 50 characters.");
            if (request.Occupation?.Length > 100) errors.Add("Occupation cannot exceed 100 characters.");
            if (request.EmergencyContactName?.Length > 100) errors.Add("EmergencyContactName cannot exceed 100 characters.");

            CustomerFieldRules.CheckPhone(errors, "Phone", request.Phone, required: true);
            CustomerFieldRules.CheckPhone(errors, "AlternativePhone", request.AlternativePhone, required: false);
            CustomerFieldRules.CheckPhone(errors, "EmergencyContactPhone", request.EmergencyContactPhone, required: false);
            CustomerFieldRules.CheckEmail(errors, request.Email);

            CustomerFieldRules.CheckMoney(errors, "CreditLimit", request.CreditLimit);
            if (request.AnnualIncome is not null) CustomerFieldRules.CheckMoney(errors, "AnnualIncome", request.AnnualIncome.Value);
            if (request.IncomeSource is not null && !Enum.IsDefined(request.IncomeSource.Value)) errors.Add("IncomeSource is not valid.");

            if (errors.Count > 0)
            {
                throw new ValidationException("The customer is not valid.", errors);
            }
        }

        private async Task EnsurePhoneNotTaken(UpdateCustomerCommand request)
        {
            // Includes soft-deleted rows because the unique index does.
            int taken = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM [Customers].[Customer] WHERE Phone = @Phone AND Id <> @Id",
                new { request.Phone, request.Id });

            if (taken > 0) throw new ConflictException("A customer with this phone number already exists.");
        }
    }
}
