namespace LoanApi.Storage
{
    public enum StorageProviderKind
    {
        Local = 1,
        Azure = 2
    }

    /// <summary>
    /// Keeps files by an opaque key that the storage generates. The caller stores the key (and which provider
    /// produced it) and never builds paths or blob names itself.
    /// </summary>
    public interface IFileStorage
    {
        StorageProviderKind Kind { get; }

        /// <summary>Saves the content under a new generated key and returns it.</summary>
        Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);

        /// <summary>Opens a stored file for reading; throws <see cref="FileNotFoundException"/> when it does not exist.</summary>
        Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);

        Task DeleteAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>Writes and removes a tiny file to prove the storage is reachable and writable.</summary>
        Task CheckAsync(CancellationToken cancellationToken = default);
    }

    public sealed class StorageException : Exception
    {
        public StorageException(string message, Exception? inner = null) : base(message, inner)
        {
        }
    }
}
