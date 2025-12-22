using Application.Features.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApi.Controllers.Modules
{
    public class AuthController : SecurityModuleController
    {
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(AuthenticateCommand request)
        {
            var res = await Mediator.Send(request);

            return Ok(res);
        }
    }
}
