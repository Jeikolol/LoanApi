using Microsoft.AspNetCore.Mvc;
using LoanApi.Common.Pagination;

namespace LoanApi.Common.Extensions
{
    public static class ControllerExtensions
    {
        public static IActionResult OkPaged<T>(this ControllerBase controller, List<T> data, PageRequest pageRequest, int totalCount)
        {
            return controller.Ok(new PagedResult<T>(data, pageRequest.PageNumber, pageRequest.PageSize, totalCount));
        }

        public static IActionResult OkPaged<T>(this ControllerBase controller, PagedResult<T> result)
        {
            return controller.Ok(result);
        }

        public static IActionResult OkPagedEmpty<T>(this ControllerBase controller, PageRequest pageRequest)
        {
            return controller.Ok(new PagedResult<T>(new List<T>(), pageRequest.PageNumber, pageRequest.PageSize, 0));
        }
    }
}
