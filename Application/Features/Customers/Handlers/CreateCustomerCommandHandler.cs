using Application.Exceptions;
using Application.Features.Customers;
using Application.Features.Customers.Commands;
using Dapper;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace Application.Features.Customers.Handlers
{
    public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CreatedCustomerResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<CreateCustomerCommandHandler> _logger;

        public CreateCustomerCommandHandler(IDbConnection connection, ILogger<CreateCustomerCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<CreatedCustomerResponse> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
        {
            Normalize(request);
            Validate(request);

            ReferenceInfo reference = await EnsureReferencesExist(request);
            ValidateIdentification(request, reference);
            await EnsureNotDuplicated(request);

            Guid id = Guid.NewGuid();

            try
            {
                await _connection.ExecuteAsync(
                    @"INSERT INTO [Customers].[Customer]
                        (Id, CustomerType, FirstName, LastName, CompanyName, IdentificationTypeId, Identification, TaxId,
                         Phone, AlternativePhone, Email, Address, Province, CountryId, NationalityCountryId, Status,
                         Occupation, AnnualIncome, IncomeSource, CreditLimit, RiskLevel, IsBlacklisted,
                         EmergencyContactName, EmergencyContactPhone, CreatedOn, CreatedById, IsActive, IsDeleted)
                      VALUES
                        (@Id, @CustomerType, @FirstName, @LastName, @CompanyName, @IdentificationTypeId, @Identification, @TaxId,
                         @Phone, @AlternativePhone, @Email, @Address, @Province, @CountryId, @NationalityCountryId, @Status,
                         @Occupation, @AnnualIncome, @IncomeSource, @CreditLimit, @RiskLevel, 0,
                         @EmergencyContactName, @EmergencyContactPhone, @CreatedOn, @CreatedById, 1, 0)",
                    new
                    {
                        Id = id,
                        CustomerType = (int)request.CustomerType,
                        request.FirstName,
                        request.LastName,
                        request.CompanyName,
                        request.IdentificationTypeId,
                        request.Identification,
                        request.TaxId,
                        request.Phone,
                        request.AlternativePhone,
                        request.Email,
                        request.Address,
                        request.Province,
                        request.CountryId,
                        request.NationalityCountryId,
                        Status = (int)CustomerStatus.Active,
                        Occupation = request.Occupation ?? string.Empty,
                        request.AnnualIncome,
                        IncomeSource = (int?)request.IncomeSource,
                        request.CreditLimit,
                        RiskLevel = (int?)request.RiskLevel,
                        request.EmergencyContactName,
                        request.EmergencyContactPhone,
                        CreatedOn = DateTime.UtcNow,
                        request.CreatedById,
                    });
            }
            catch (DbException ex) when (ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                // Two requests passed the duplicate check at the same time; the unique index decides.
                throw new ConflictException("A customer with the same identification or phone already exists.");
            }

            _logger.LogInformation("Customer {CustomerId} created", id);

            return new CreatedCustomerResponse(id);
        }

        private static void Normalize(CreateCustomerCommand request)
        {
            request.FirstName = request.FirstName.Trim();
            request.LastName = request.LastName.Trim();
            request.Identification = request.Identification.Trim();
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

        private void Validate(CreateCustomerCommand request)
        {
            List<string> errors = [];

            CustomerFieldRules.Required(errors, "FirstName", request.FirstName, 100);
            CustomerFieldRules.Required(errors, "LastName", request.LastName, 100);
            CustomerFieldRules.Required(errors, "Identification", request.Identification, 50);
            CustomerFieldRules.Required(errors, "Address", request.Address, 250);
            CustomerFieldRules.Required(errors, "Province", request.Province, 100);

            if (!Enum.IsDefined(request.CustomerType)) errors.Add("CustomerType is not valid.");
            if (request.CustomerType == CustomerType.LegalPerson && request.CompanyName is null) errors.Add("CompanyName is required for a legal person.");
            if (request.CompanyName?.Length > 200) errors.Add("CompanyName cannot exceed 200 characters.");
            if (request.TaxId?.Length > 50) errors.Add("TaxId cannot exceed 50 characters.");
            if (request.Occupation?.Length > 100) errors.Add("Occupation cannot exceed 100 characters.");
            if (request.EmergencyContactName?.Length > 100) errors.Add("EmergencyContactName cannot exceed 100 characters.");

            if (request.IdentificationTypeId == Guid.Empty) errors.Add("IdentificationTypeId is required.");
            if (request.CountryId == Guid.Empty) errors.Add("CountryId is required.");
            if (request.NationalityCountryId == Guid.Empty) errors.Add("NationalityCountryId is required.");

            CustomerFieldRules.CheckPhone(errors, "Phone", request.Phone, required: true);
            CustomerFieldRules.CheckPhone(errors, "AlternativePhone", request.AlternativePhone, required: false);
            CustomerFieldRules.CheckPhone(errors, "EmergencyContactPhone", request.EmergencyContactPhone, required: false);

            CustomerFieldRules.CheckEmail(errors, request.Email);

            CustomerFieldRules.CheckMoney(errors, "CreditLimit", request.CreditLimit);
            if (request.AnnualIncome is not null) CustomerFieldRules.CheckMoney(errors, "AnnualIncome", request.AnnualIncome.Value);

            if (request.IncomeSource is not null && !Enum.IsDefined(request.IncomeSource.Value)) errors.Add("IncomeSource is not valid.");
            if (request.RiskLevel is not null && !Enum.IsDefined(request.RiskLevel.Value)) errors.Add("RiskLevel is not valid.");

            if (errors.Count > 0)
            {
                throw new ValidationException("The customer is not valid.", errors);
            }
        }

        private sealed record ReferenceInfo(string IdentificationTypeCode, string? NationalityIso2);

        private async Task<ReferenceInfo> EnsureReferencesExist(CreateCustomerCommand request)
        {
            string? typeCode = await _connection.QueryFirstOrDefaultAsync<string>(
                "SELECT Code FROM [Admin].[IdentificationType] WHERE Id = @Id AND IsDeleted = 0",
                new { Id = request.IdentificationTypeId });

            List<string> errors = [];
            if (typeCode is null) errors.Add("The identification type does not exist.");

            Guid[] countryIds = [.. new[] { request.CountryId, request.NationalityCountryId }.Distinct()];
            foreach (Guid countryId in countryIds)
            {
                int countries = await _connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM [Reference].[Country] WHERE Id = @Id AND IsDeleted = 0", new { Id = countryId });

                if (countries == 0)
                {
                    errors.Add(countryId == request.CountryId ? "The country does not exist." : "The nationality country does not exist.");
                }
            }

            if (errors.Count > 0)
            {
                throw new ValidationException("The customer is not valid.", errors);
            }

            string? nationalityIso2 = await _connection.QueryFirstOrDefaultAsync<string>(
                "SELECT CodeIso2 FROM [Reference].[Country] WHERE Id = @Id AND IsDeleted = 0",
                new { Id = request.NationalityCountryId });

            return new ReferenceInfo(typeCode!, nationalityIso2);
        }

        private static void ValidateIdentification(CreateCustomerCommand request, ReferenceInfo reference)
        {
            List<string> errors = [];

            errors.AddRange(IdentificationRules.ValidateCombination(
                request.CustomerType, reference.IdentificationTypeCode, reference.NationalityIso2));

            request.Identification = IdentificationRules.Normalize(reference.IdentificationTypeCode, request.Identification);

            string? problem = IdentificationRules.Validate(reference.IdentificationTypeCode, request.Identification, reference.NationalityIso2);
            if (problem is not null) errors.Add(problem);

            if (errors.Count > 0)
            {
                throw new ValidationException("The customer is not valid.", errors);
            }
        }

        private async Task EnsureNotDuplicated(CreateCustomerCommand request)
        {
            // Includes soft-deleted rows because the unique indexes do.
            IEnumerable<(string Identification, string Phone)> matches = await _connection.QueryAsync<(string, string)>(
                "SELECT Identification, Phone FROM [Customers].[Customer] WHERE Identification = @Identification OR Phone = @Phone",
                new { request.Identification, request.Phone });

            List<string> conflicts = [];
            foreach ((string identification, string phone) in matches)
            {
                if (identification == request.Identification) conflicts.Add("A customer with this identification already exists.");
                if (phone == request.Phone) conflicts.Add("A customer with this phone number already exists.");
            }

            if (conflicts.Count > 0)
            {
                throw new ConflictException(string.Join(" ", conflicts.Distinct()));
            }
        }
    }
}
