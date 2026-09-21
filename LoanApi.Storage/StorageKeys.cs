using System.Text.RegularExpressions;

namespace LoanApi.Storage
{
    /// <summary>The shape of the keys every provider hands out, and the checks that keep callers from forging others.</summary>
    public static partial class StorageKeys
    {
        [GeneratedRegex(@"^\d{4}/\d{2}/[0-9a-f]{32}\.[a-z0-9]{1,5}$")]
        private static partial Regex KeyPattern();

        [GeneratedRegex(@"^[a-z0-9]{1,5}$")]
        private static partial Regex ExtensionPattern();

        public static bool IsValid(string? key) => key is not null && KeyPattern().IsMatch(key);

        /// <summary>Lower-cases the extension and rejects anything that is not a short alphanumeric suffix.</summary>
        public static string NormalizeExtension(string? extension)
        {
            string cleaned = (extension ?? string.Empty).TrimStart('.').ToLowerInvariant();
            if (!ExtensionPattern().IsMatch(cleaned))
            {
                throw new ArgumentException("The file extension is not valid.", nameof(extension));
            }

            return cleaned;
        }

        /// <summary>A new key such as 2026/09/3f2b...c1.pdf. The file name the user chose never takes part in it.</summary>
        public static string Create(string extension, DateTime utcNow)
        {
            return $"{utcNow:yyyy}/{utcNow:MM}/{Guid.NewGuid():N}.{NormalizeExtension(extension)}";
        }
    }
}
