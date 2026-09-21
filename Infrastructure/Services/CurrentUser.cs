using Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId =>
            Guid.TryParse(_httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value, out Guid id) ? id : null;

        public string? UserName => _httpContextAccessor.HttpContext?.User.FindFirst("unique_name")?.Value;

        public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
