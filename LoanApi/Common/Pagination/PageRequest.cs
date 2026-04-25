namespace LoanApi.Common.Pagination
{
    public class PageRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public PageRequest() { }

        public PageRequest(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber > 0 ? pageNumber : 1;
            PageSize = pageSize > 0 && pageSize <= 100 ? pageSize : 10;
        }

        public int GetSkip() => (PageNumber - 1) * PageSize;
    }
}
