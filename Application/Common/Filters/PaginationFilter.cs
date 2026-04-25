namespace Application.Common.Filters
{
    public class PaginationFilter
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public PaginationFilter() { }

        public PaginationFilter(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber > 0 ? pageNumber : 1;
            PageSize = pageSize > 0 && pageSize <= 100 ? pageSize : 10;
        }

        public int GetSkip() => (PageNumber - 1) * PageSize;
    }
}
