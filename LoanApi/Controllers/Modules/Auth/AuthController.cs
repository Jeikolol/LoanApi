using Application.Features.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LoanApi.Controllers.Modules.Auth
{
    public class AuthController : SecurityModuleController
    {
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(AuthenticateCommand request)
        {
            var res = await Mediator.Send(request);

            return Ok(res);
        }

        [HttpPost("change-password")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            if (!Guid.TryParse(User.FindFirst("sub")?.Value, out Guid userId))
            {
                return Unauthorized();
            }

            command.UserId = userId;

            return Ok(await Mediator.Send(command));
        }
    }
}
