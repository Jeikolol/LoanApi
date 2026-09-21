using Application.Exceptions;
using Application.Services;
using Dapper;
using LoanApi.Storage;
using MediatR;
using System.Data;

namespace Application.Features.Storage
{
    public record StorageSettingsResponse(
        bool AzureEnabled,
        string? AzureAccountName,
        string? AzureContainer,
        string AzureCredentialSource,
        string ActiveProvider);

    public record GetStorageSettingsQuery : IRequest<StorageSettingsResponse>;

    /// <summary>Switches Azure on or off and sets which account and container it uses. No secret is part of it.</summary>
    public class UpdateStorageSettingsCommand : IRequest<StorageSettingsResponse>
    {
        public bool AzureEnabled { get; set; }
        public string? AzureAccountName { get; set; }
        public string? AzureContainer { get; set; }
    }

    public class GetStorageSettingsQueryHandler : IRequestHandler<GetStorageSettingsQuery, StorageSettingsResponse>
    {
        private readonly IFileStorageFactory _factory;

        public GetStorageSettingsQueryHandler(IFileStorageFactory factory) => _factory = factory;

        public async Task<StorageSettingsResponse> Handle(GetStorageSettingsQuery request, CancellationToken cancellationToken)
            => await StorageSettingsMapper.Read(_factory, cancellationToken);
    }

    public class UpdateStorageSettingsCommandHandler : IRequestHandler<UpdateStorageSettingsCommand, StorageSettingsResponse>
    {
        private readonly IDbConnection _connection;
        private readonly IFileStorageFactory _factory;

        public UpdateStorageSettingsCommandHandler(IDbConnection connection, IFileStorageFactory factory)
        {
            _connection = connection;
            _factory = factory;
        }

        public async Task<StorageSettingsResponse> Handle(UpdateStorageSettingsCommand request, CancellationToken cancellationToken)
        {
            string? account = Clean(request.AzureAccountName);
            string? container = Clean(request.AzureContainer);

            List<string> errors = [];
            if (account is not null && !StorageSelection.IsValidAccountName(account))
            {
                errors.Add("The account name must be 3 to 24 lower-case letters or digits.");
            }

            if (container is not null && !StorageSelection.IsValidContainerName(container))
            {
                errors.Add("The container name must be 3 to 63 lower-case letters, digits or single hyphens.");
            }

            if (request.AzureEnabled && (account is null || container is null))
            {
                errors.Add("The account and the container are required to enable Azure.");
            }

            if (errors.Count > 0) throw new ValidationException("The storage settings are not valid.", errors);

            if (request.AzureEnabled)
            {
                // Never switch to a storage that cannot take a file: prove it before it becomes the active one.
                try
                {
                    await _factory.CreateAzure(account!, container!).CheckAsync(cancellationToken);
                }
                catch (StorageException ex)
                {
                    throw new ValidationException("Azure storage cannot be enabled.", [ex.InnerException?.Message is { } inner ? $"{ex.Message} ({FirstLine(inner)})" : ex.Message]);
                }
            }

            int updated = await _connection.ExecuteAsync(
                @"UPDATE [Admin].[StorageSetting]
                  SET AzureEnabled = @AzureEnabled, AzureAccountName = @Account, AzureContainer = @Container, UpdatedOn = @Now
                  WHERE IsDeleted = 0",
                new { request.AzureEnabled, Account = account, Container = container, Now = DateTime.UtcNow });

            if (updated == 0)
            {
                await _connection.ExecuteAsync(
                    @"INSERT INTO [Admin].[StorageSetting]
                        (Id, AzureEnabled, AzureAccountName, AzureContainer, CreatedOn, IsActive, IsDeleted)
                      VALUES (@Id, @AzureEnabled, @Account, @Container, @Now, 1, 0)",
                    new { Id = Guid.NewGuid(), request.AzureEnabled, Account = account, Container = container, Now = DateTime.UtcNow });
            }

            return await StorageSettingsMapper.Read(_factory, cancellationToken);
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string FirstLine(string text) => text.Split('\n')[0].Trim();
    }

    internal static class StorageSettingsMapper
    {
        public static async Task<StorageSettingsResponse> Read(IFileStorageFactory factory, CancellationToken cancellationToken)
        {
            StorageSettings settings = await factory.GetSettingsAsync(cancellationToken);
            StorageProviderKind active = await factory.GetActiveKindAsync(cancellationToken);

            return new StorageSettingsResponse(
                settings.AzureEnabled, settings.AzureAccountName, settings.AzureContainer,
                factory.AzureCredentialSource, active.ToString());
        }
    }
}
