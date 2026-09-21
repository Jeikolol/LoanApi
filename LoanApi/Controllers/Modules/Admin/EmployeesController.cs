using Application.Features.Employees;
using Domain.Constants;
using LoanApi.Common.Extensions;
using LoanApi.Common.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApi.Controllers.Modules.Admin
{
    /// <summary>Employee accounts and their roles. Only administrators can manage them.</summary>
    [Authorize(Roles = SystemRoles.Admin)]
    [Route("api/v{version:apiVersion}/employees")]
    public class EmployeesController : AdminModuleController
    {
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await Mediator.Send(new GetEmployeesQuery(pageNumber, pageSize));
            return this.OkPaged(result.Data, new PageRequest(pageNumber, pageSize), result.TotalCount);
        }

        /// <summary>Roles and branches to choose from</summary>
        [HttpGet("GetOptions")]
        public async Task<IActionResult> GetOptions()
        {
            var result = await Mediator.Send(new GetEmployeeOptionsQuery());
            return Ok(result);
        }

        /// <summary>
        /// Create an employee. The response carries a temporary password, once: it must be changed at first sign-in.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeCommand command)
        {
            var result = await Mediator.Send(command);
            return NoStore(Ok(result));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeCommand command)
        {
            command.Id = id;
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("Activate/{id:guid}")]
        public async Task<IActionResult> Activate(Guid id)
        {
            var result = await Mediator.Send(new SetEmployeeActiveCommand { Id = id, Active = true });
            return Ok(result);
        }

        /// <summary>Deactivate an employee: they cannot sign in and their current sessions stop working.</summary>
        [HttpPost("Deactivate/{id:guid}")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var result = await Mediator.Send(new SetEmployeeActiveCommand { Id = id, Active = false });
            return Ok(result);
        }

        /// <summary>Issue a new temporary password (shown once) and end the employee's current sessions.</summary>
        [HttpPost("ResetPassword/{id:guid}")]
        public async Task<IActionResult> ResetPassword(Guid id)
        {
            var result = await Mediator.Send(new ResetEmployeePasswordCommand { Id = id });
            return NoStore(Ok(result));
        }

        // A response that carries a password must never be kept by a browser or a proxy.
        private IActionResult NoStore(IActionResult result)
        {
            Response.Headers.CacheControl = "no-store";
            Response.Headers.Pragma = "no-cache";
            return result;
        }
    }
}
