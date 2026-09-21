namespace Domain.Entities
{
    /// <summary>
    /// The single row that decides where new files go. It holds no secrets: the Azure credential comes from the
    /// server (managed identity or a connection string in its configuration).
    /// </summary>
    public class StorageSetting : AuditableEntity<Guid>
    {
        public bool AzureEnabled { get; set; }
        public string? AzureAccountName { get; set; }
        public string? AzureContainer { get; set; }
    }
}
