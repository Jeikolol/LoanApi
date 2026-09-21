using System.Text.RegularExpressions;

namespace LoanApi.Storage
{
    /// <summary>Which provider takes new files, and the naming rules Azure imposes on the settings that select it.</summary>
    public static partial class StorageSelection
    {
        // https://learn.microsoft.com/azure/storage/common/storage-account-overview (naming rules)
        [GeneratedRegex(@"^[a-z0-9]{3,24}$")]
        private static partial Regex AccountPattern();

        [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
        private static partial Regex ContainerPattern();

        public static bool IsValidAccountName(string? name) => name is not null && AccountPattern().IsMatch(name);

        public static bool IsValidContainerName(string? name) =>
            name is not null && name.Length is >= 3 and <= 63 && ContainerPattern().IsMatch(name);

        /// <summary>
        /// Azure takes new files only when it is switched on and fully configured; in every other case files go to
        /// local storage, so an upload never fails just because Azure is off or half set up.
        /// </summary>
        public static StorageProviderKind ForNewFiles(bool azureEnabled, string? accountName, string? containerName) =>
            azureEnabled && IsValidAccountName(accountName) && IsValidContainerName(containerName)
                ? StorageProviderKind.Azure
                : StorageProviderKind.Local;
    }
}
