using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymCrudApp.ViewModels.Members
{
    public class MemberIndexViewModel
    {
        public List<MemberListItemViewModel> Members { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? Plan { get; set; }
        public IEnumerable<SelectListItem> PlanChoices { get; set; } = new List<SelectListItem>();

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public bool HasFilters =>
            !string.IsNullOrWhiteSpace(SearchTerm)
            || !string.IsNullOrWhiteSpace(Status)
            || !string.IsNullOrWhiteSpace(Plan);
    }
}
