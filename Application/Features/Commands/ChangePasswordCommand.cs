using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Commands
{
    public class ChangePasswordCommand : IRequest<TokenResponse>
    {
        [JsonIgnore]
        public Guid UserId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
