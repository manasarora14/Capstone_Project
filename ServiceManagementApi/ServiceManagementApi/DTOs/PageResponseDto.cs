using ServiceManagementApi.Models;

namespace ServiceManagementApi.DTOs
{
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class QueryParameters
    {
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public RequestStatus? StatusFilter { get; set; }
    }
}
