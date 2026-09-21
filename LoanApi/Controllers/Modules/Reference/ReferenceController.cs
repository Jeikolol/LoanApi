using Application.Features.Reference;
using Application.Features.Reference.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApi.Controllers.Modules.Reference
{
    [Authorize]
    [Route("api/v{version:apiVersion}/reference")]
    public class ReferenceController : ReferenceModuleController
    {
        /// <summary>
        /// Identification document types (cédula, passport, ...)
        /// </summary>
        [HttpGet("identification-types")]
        public async Task<IActionResult> GetIdentificationTypes() => Ok(await Mediator.Send(new GetIdentificationTypesQuery()));

        /// <summary>
        /// Countries with their nationality names
        /// </summary>
        [HttpGet("countries")]
        public async Task<IActionResult> GetCountries() => Ok(await Mediator.Send(new GetCountriesQuery()));

        /// <summary>
        /// Branches a loan can be booked in
        /// </summary>
        [HttpGet("branches")]
        public async Task<IActionResult> GetBranches() => Ok(await Mediator.Send(new GetBranchesQuery()));
    }
}
