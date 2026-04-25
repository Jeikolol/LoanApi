using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoanApi.Controllers.Modules.Customers.Requests;
using Application.Features.Customers.Queries;
using Application.Features.Customers.Commands;

namespace LoanApi.Controllers.Modules.Customers
{
    [Authorize]
    [Route("customers")]
    public class CustomersController : CustomerModuleController
    {
        /// <summary>
        /// Get customer by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var query = new GetCustomerByIdQuery(id);
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Update customer
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request)
        {
            var command = new UpdateCustomerCommand
            {
                Id = id,
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                Email = request.Email,
                Phone = request.PhoneNumber ?? string.Empty
            };
            var result = await Mediator.Send(command);
            return Ok(result);
        }
    }
}
