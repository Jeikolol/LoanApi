using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using LoanApi.Controllers.Modules.Customers.Requests;
using LoanApi.Common.Pagination;
using LoanApi.Common.Extensions;
using Application.Features.Customers.Queries;
using Application.Features.Customers.Commands;

namespace LoanApi.Controllers.Modules.Customers
{
    [Authorize]
    public class CustomersController : CustomerModuleController
    {
        /// <summary>
        /// Get all customers (paginated)
        /// </summary>
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var query = new GetCustomersQuery { PageNumber = pageNumber, PageSize = pageSize, Search = search };
            var result = await Mediator.Send(query);
            return this.OkPaged(result.Data, new PageRequest(result.PageNumber, result.PageSize), result.TotalCount);
        }

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
        /// Create customer
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command)
        {
            if (Guid.TryParse(User.FindFirst("sub")?.Value, out Guid userId))
            {
                command.CreatedById = userId;
            }

            CreatedCustomerResponse result = await Mediator.Send(command);
            return Created($"{Request.Path}/{result.Id}", result);
        }

        /// <summary>
        /// Update customer
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = SystemRoles.AdminOrManager)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request)
        {
            var command = new UpdateCustomerCommand
            {
                Id = id,
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                CompanyName = request.CompanyName,
                Email = request.Email,
                Phone = request.PhoneNumber ?? string.Empty,
                AlternativePhone = request.AlternativePhone,
                Address = request.Address ?? string.Empty,
                Province = request.Province ?? string.Empty,
                Occupation = request.Occupation,
                AnnualIncome = request.AnnualIncome,
                IncomeSource = request.IncomeSource,
                CreditLimit = request.CreditLimit,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactPhone = request.EmergencyContactPhone,
                TaxId = request.TaxId
            };
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Put a customer on the blacklist; they cannot receive loans until removed
        /// </summary>
        [HttpPost("Blacklist/{id:guid}")]
        [Authorize(Roles = SystemRoles.AdminOrManager)]
        public async Task<IActionResult> Blacklist(Guid id, [FromBody] BlacklistCustomerRequest request) =>
            Ok(await Mediator.Send(new SetCustomerBlacklistCommand { Id = id, Blacklisted = true, Reason = request.Reason }));

        /// <summary>
        /// Take a customer off the blacklist
        /// </summary>
        [HttpPost("Unblacklist/{id:guid}")]
        [Authorize(Roles = SystemRoles.AdminOrManager)]
        public async Task<IActionResult> Unblacklist(Guid id) =>
            Ok(await Mediator.Send(new SetCustomerBlacklistCommand { Id = id, Blacklisted = false }));
    }
}
