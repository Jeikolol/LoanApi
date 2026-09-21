namespace Application.Models.Responses
{
    public record AuditLogResponse(
        Guid Id,
        DateTime OccurredOn,
        Guid? UserId,
        string? UserName,
        string Action,
        string? Payload,
        bool Succeeded,
        string? Error,
        string? IpAddress,
        int DurationMs
    );
}
