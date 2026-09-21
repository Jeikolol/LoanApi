namespace LoanApi.Storage
{
    public sealed class LocalStorageOptions
    {
        /// <summary>Folder that holds the files. It should be outside any folder the web server publishes.</summary>
        public string RootPath { get; set; } = string.Empty;
    }

    public sealed class LocalFileStorage : IFileStorage
    {
        private readonly string _root;

        public LocalFileStorage(LocalStorageOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.RootPath))
            {
                throw new StorageException("The local storage folder is not configured.");
            }

            _root = Path.GetFullPath(options.RootPath);
        }

        public StorageProviderKind Kind => StorageProviderKind.Local;

        public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
        {
            string key = StorageKeys.Create(extension, DateTime.UtcNow);
            string path = ResolvePath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            try
            {
                await using FileStream file = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                await content.CopyToAsync(file, cancellationToken);
            }
            catch
            {
                // Never leave half a file behind.
                if (File.Exists(path)) File.Delete(path);
                throw;
            }

            return key;
        }

        public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        {
            string path = ResolvePath(key);
            if (!File.Exists(path)) throw new FileNotFoundException("The stored file does not exist.", key);

            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            string path = ResolvePath(key);
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }

        public async Task CheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using MemoryStream probe = new([0x4f, 0x4b]);
                string key = await SaveAsync(probe, "tmp", cancellationToken);
                await DeleteAsync(key, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new StorageException("The local storage folder is not writable.", ex);
            }
        }

        /// <summary>Only keys this library generates are accepted, and the result is always inside the root folder.</summary>
        private string ResolvePath(string key)
        {
            if (!StorageKeys.IsValid(key))
            {
                throw new ArgumentException("The storage key is not valid.", nameof(key));
            }

            string path = Path.GetFullPath(Path.Combine(_root, key));
            if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                throw new ArgumentException("The storage key is not valid.", nameof(key));
            }

            return path;
        }
    }
}
