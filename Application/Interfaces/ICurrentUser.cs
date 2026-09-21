namespace Application.Interfaces
{
    public interface ICurrentUser : IScopedService
    {
        Guid? UserId { get; }
        string? UserName { get; }
        string? IpAddress { get; }
    }
}
