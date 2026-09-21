using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace LoanApi.Storage
{
    /// <summary>
    /// The server-side part of the Azure configuration. The secret never lives in the database: either a
    /// connection string from server configuration or, preferably, the managed identity of the host.
    /// </summary>
    public sealed class AzureCredentialOptions
    {
        /// <summary>Optional. When empty, the host's managed identity (DefaultAzureCredential) is used.</summary>
        public string? ConnectionString { get; set; }

        public bool HasConnectionString => !string.IsNullOrWhiteSpace(ConnectionString);
    }

    public sealed class AzureBlobFileStorage : IFileStorage
    {
        private readonly BlobContainerClient _container;

        public AzureBlobFileStorage(string accountName, string containerName, AzureCredentialOptions credentials)
        {
            if (!StorageSelection.IsValidAccountName(accountName) || !StorageSelection.IsValidContainerName(containerName))
            {
                throw new StorageException("The Azure account or container name is not valid.");
            }

            _container = credentials.HasConnectionString
                ? new BlobContainerClient(credentials.ConnectionString, containerName)
                : new BlobContainerClient(
                    new Uri($"https://{accountName}.blob.core.windows.net/{containerName}"),
                    new DefaultAzureCredential());
        }

        public StorageProviderKind Kind => StorageProviderKind.Azure;

        public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
        {
            string key = StorageKeys.Create(extension, DateTime.UtcNow);

            try
            {
                await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
                await _container.GetBlobClient(key).UploadAsync(content, overwrite: false, cancellationToken);
            }
            catch (RequestFailedException ex)
            {
                throw new StorageException("The file could not be saved to Azure storage.", ex);
            }

            return key;
        }

        public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        {
            EnsureValid(key);

            try
            {
                return await _container.GetBlobClient(key).OpenReadAsync(cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                throw new FileNotFoundException("The stored file does not exist.", key, ex);
            }
            catch (RequestFailedException ex)
            {
                throw new StorageException("The file could not be read from Azure storage.", ex);
            }
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            EnsureValid(key);

            try
            {
                await _container.GetBlobClient(key).DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex)
            {
                throw new StorageException("The file could not be deleted from Azure storage.", ex);
            }
        }

        public async Task CheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using MemoryStream probe = new([0x4f, 0x4b]);
                string key = await SaveAsync(probe, "tmp", cancellationToken);
                await DeleteAsync(key, cancellationToken);
            }
            catch (Exception ex) when (ex is AuthenticationFailedException or CredentialUnavailableException)
            {
                throw new StorageException("No valid Azure credentials are available on the server.", ex);
            }
        }

        private static void EnsureValid(string key)
        {
            if (!StorageKeys.IsValid(key)) throw new ArgumentException("The storage key is not valid.", nameof(key));
        }
    }
}
