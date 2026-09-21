using Application.Exceptions;
using Application.Features.Customers.Commands;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Customers.Handlers
{
    public class SetCustomerBlacklistCommandHandler : IRequestHandler<SetCustomerBlacklistCommand, CustomerDetailResponse>
    {
        public const int MinReasonLength = 5;
        public const int MaxReasonLength = 250;

        private readonly IDbConnection _connection;
        private readonly ILogger<SetCustomerBlacklistCommandHandler> _logger;

        public SetCustomerBlacklistCommandHandler(IDbConnection connection, ILogger<SetCustomerBlacklistCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<CustomerDetailResponse> Handle(SetCustomerBlacklistCommand request, CancellationToken cancellationToken)
        {
            string? reason = request.Blacklisted ? (request.Reason ?? string.Empty).Trim() : null;
            if (request.Blacklisted && reason!.Length is < MinReasonLength or > MaxReasonLength)
            {
                throw new ValidationException(
                    "The blacklist change is not valid.",
                    [$"The reason is required: between {MinReasonLength} and {MaxReasonLength} characters."]);
            }

            bool? current = await _connection.QuerySingleOrDefaultAsync<bool?>(
                "SELECT IsBlacklisted FROM [Customers].[Customer] WHERE Id = @Id AND IsDeleted = 0", new { request.Id });
            if (current is null) throw new NotFoundException($"Customer {request.Id} not found");

            // The IsBlacklisted guard makes a double click or two people acting at once change it only once.
            int updated = await _connection.ExecuteAsync(
                @"UPDATE [Customers].[Customer]
                  SET IsBlacklisted = @Blacklisted, BlacklistReason = @Reason, UpdatedOn = @Now
                  WHERE Id = @Id AND IsDeleted = 0 AND IsBlacklisted <> @Blacklisted",
                new { request.Id, request.Blacklisted, Reason = reason, Now = DateTime.UtcNow });

            if (updated != 1)
            {
                throw new ConflictException(request.Blacklisted
                    ? "The customer is already blacklisted."
                    : "The customer is not blacklisted.");
            }

            _logger.LogInformation("Customer {CustomerId} {Action} the blacklist", request.Id, request.Blacklisted ? "added to" : "removed from");

            return await _connection.QuerySingleAsync<CustomerDetailResponse>(
                @"SELECT c.Id, c.CustomerType, c.FirstName, c.LastName, c.CompanyName, c.Identification, c.IdentificationTypeId,
                         c.Phone, c.Email, c.Address, c.Province, c.CountryId, c.Status, c.Occupation, c.AnnualIncome,
                         c.IncomeSource, c.CreditLimit, c.RiskLevel, c.IsBlacklisted, c.BlacklistReason, c.TaxId,
                         c.AlternativePhone, c.EmergencyContactName, c.EmergencyContactPhone, c.CreatedOn, c.UpdatedOn
                  FROM [Customers].[Customer] c WHERE c.Id = @Id",
                new { request.Id });
        }
    }
}
