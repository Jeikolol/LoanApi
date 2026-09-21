using Application.Common.Filters;
using Application.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Extensions
{
    public static class PaginationQueryableExtensions
    {
        private const int MaxPageSize = 100;

        /// <summary>
        /// Counts and pages an already ordered, already projected query into a <see cref="PaginatedResponse{T}"/>.
        /// Page number and size are normalized here (page >= 1, size between 1 and 100), and the response
        /// carries the values that were actually applied.
        /// </summary>
        public static async Task<PaginatedResponse<T>> ToPaginatedResponseAsync<T>(
            this IQueryable<T> query,
            PaginationFilter filter,
            CancellationToken cancellationToken = default)
        {
            var pageNumber = Math.Max(filter.PageNumber, 1);
            var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResponse<T>(items, pageNumber, pageSize, totalCount);
        }
    }
}
