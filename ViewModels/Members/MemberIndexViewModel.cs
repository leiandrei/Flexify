using System.Collections.Generic;

namespace GymCrudApp.ViewModels.Members
{
    public class MemberIndexViewModel
    {
        public List<MemberListItemViewModel> Members { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}