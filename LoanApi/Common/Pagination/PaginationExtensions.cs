using Microsoft.EntityFrameworkCore;

namespace LoanApi.Common.Pagination
{
    public static class PaginationExtensions
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            PageRequest pageRequest)
        {
            var totalCount = query.Count();
            var data = await query
                .Skip(pageRequest.GetSkip())
                .Take(pageRequest.PageSize)
                .ToListAsync();

            return new PagedResult<T>(data, pageRequest.PageNumber, pageRequest.PageSize, totalCount);
        }

        public static PagedResult<T> ToPagedResult<T>(
            this IQueryable<T> query,
            PageRequest pageRequest)
        {
            var totalCount = query.Count();
            var data = query
                .Skip(pageRequest.GetSkip())
                .Take(pageRequest.PageSize)
                .ToList();

            return new PagedResult<T>(data, pageRequest.PageNumber, pageRequest.PageSize, totalCount);
        }
    }
}
