using System;
namespace NewAppErp.Models.Employer
{ 
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}