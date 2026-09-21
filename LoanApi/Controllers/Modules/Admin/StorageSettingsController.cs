using Application.Features.Storage;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApi.Controllers.Modules.Admin
{
    [Authorize(Roles = SystemRoles.AdminOrManager)]
    [Route("api/v{version:apiVersion}/storage-settings")]
    public class StorageSettingsController : AdminModuleController
    {
        /// <summary>
        /// Where new documents are stored, and whether Azure is on
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await Mediator.Send(new GetStorageSettingsQuery());
            return Ok(result);
        }

        /// <summary>
        /// Switch Azure storage on or off. Enabling it first checks that a test file can be written there.
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateStorageSettingsCommand command)
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }
    }
}
