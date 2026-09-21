using Dapper;
using Domain.Enums;
using LoanApi.Storage;
using System.Data;

namespace Application.Services
{
    /// <summary>Server-side storage configuration (never stored in the database, so it never holds a secret there).</summary>
    public sealed class StorageConfiguration
    {
        public LocalStorageOptions Local { get; set; } = new();
        public AzureCredentialOptions Azure { get; set; } = new();
    }

    public sealed record StorageSettings(bool AzureEnabled, string? AzureAccountName, string? AzureContainer);

    public interface IFileStorageFactory
    {
        /// <summary>Where the Azure credential comes from: "ConnectionString" or "ManagedIdentity".</summary>
        string AzureCredentialSource { get; }

        Task<StorageSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

        /// <summary>The provider that takes new files right now (Azure when on and complete, otherwise local).</summary>
        Task<StorageProviderKind> GetActiveKindAsync(CancellationToken cancellationToken = default);

        Task<IFileStorage> GetForNewFilesAsync(CancellationToken cancellationToken = default);

        /// <summary>The storage that holds an existing file, whatever the current setting is.</summary>
        Task<IFileStorage> GetForExistingAsync(DocumentStorage provider, CancellationToken cancellationToken = default);

        IFileStorage CreateAzure(string accountName, string containerName);
    }

    public sealed class FileStorageFactory : IFileStorageFactory
    {
        private readonly IDbConnection _connection;
        private readonly StorageConfiguration _configuration;

        public FileStorageFactory(IDbConnection connection, StorageConfiguration configuration)
        {
            _connection = connection;
            _configuration = configuration;
        }

        public string AzureCredentialSource => _configuration.Azure.HasConnectionString ? "ConnectionString" : "ManagedIdentity";

        public async Task<StorageSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            // A single row; it may not exist yet on a fresh database, which means "Azure off".
            SettingsRow? row = await _connection.QueryFirstOrDefaultAsync<SettingsRow>(
                "SELECT AzureEnabled, AzureAccountName, AzureContainer FROM [Admin].[StorageSetting] WHERE IsDeleted = 0");

            return row is null ? new StorageSettings(false, null, null) : new StorageSettings(row.AzureEnabled, row.AzureAccountName, row.AzureContainer);
        }

        public async Task<StorageProviderKind> GetActiveKindAsync(CancellationToken cancellationToken = default)
        {
            StorageSettings settings = await GetSettingsAsync(cancellationToken);
            return StorageSelection.ForNewFiles(settings.AzureEnabled, settings.AzureAccountName, settings.AzureContainer);
        }

        public async Task<IFileStorage> GetForNewFilesAsync(CancellationToken cancellationToken = default)
        {
            StorageSettings settings = await GetSettingsAsync(cancellationToken);
            StorageProviderKind kind = StorageSelection.ForNewFiles(settings.AzureEnabled, settings.AzureAccountName, settings.AzureContainer);

            return kind == StorageProviderKind.Azure
                ? CreateAzure(settings.AzureAccountName!, settings.AzureContainer!)
                : CreateLocal();
        }

        public async Task<IFileStorage> GetForExistingAsync(DocumentStorage provider, CancellationToken cancellationToken = default)
        {
            switch (provider)
            {
                case DocumentStorage.Local:
                    return CreateLocal();

                case DocumentStorage.Azure:
                    // Files already in Azure stay readable after Azure is switched off for new files.
                    StorageSettings settings = await GetSettingsAsync(cancellationToken);
                    if (!StorageSelection.IsValidAccountName(settings.AzureAccountName) || !StorageSelection.IsValidContainerName(settings.AzureContainer))
                    {
                        throw new StorageException("The Azure storage that holds this file is not configured.");
                    }

                    return CreateAzure(settings.AzureAccountName!, settings.AzureContainer!);

                default:
                    throw new StorageException("This document is an external reference and has no stored file.");
            }
        }

        public IFileStorage CreateAzure(string accountName, string containerName) =>
            new AzureBlobFileStorage(accountName, containerName, _configuration.Azure);

        private IFileStorage CreateLocal()
        {
            string root = string.IsNullOrWhiteSpace(_configuration.Local.RootPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LoanApi", "files")
                : _configuration.Local.RootPath;

            return new LocalFileStorage(new LocalStorageOptions { RootPath = root });
        }

        private sealed class SettingsRow
        {
            public bool AzureEnabled { get; set; }
            public string? AzureAccountName { get; set; }
            public string? AzureContainer { get; set; }
        }
    }
}
