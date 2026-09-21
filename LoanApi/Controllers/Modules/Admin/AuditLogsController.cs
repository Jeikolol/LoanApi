using Application.Features.Audit.Queries;
using Domain.Constants;
using LoanApi.Common.Extensions;
using LoanApi.Common.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApi.Controllers.Modules.Admin
{
    [Authorize(Roles = SystemRoles.AdminOrManager)]
    [Route("api/v{version:apiVersion}/audit-logs")]
    public class AuditLogsController : AdminModuleController
    {
        /// <summary>
        /// Audit trail of every command executed in the system (paginated, newest first)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? user = null,
            [FromQuery] string? action = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] bool? succeeded = null)
        {
            var query = new GetAuditLogsQuery(pageNumber, pageSize) { User = user, Action = action, From = from, To = to, Succeeded = succeeded };
            var result = await Mediator.Send(query);
            return this.OkPaged(result.Data, new PageRequest(result.PageNumber, result.PageSize), result.TotalCount);
        }
    }
}
