using System;
using System.Collections.Generic;

namespace GymCrudApp.ViewModels.Attendance
{
    public class AttendanceIndexViewModel
    {
        public string? SearchQuery { get; set; }
        public DateTime FilterDate { get; set; } = DateTime.Today;
        public int OnFloorCount { get; set; }
        public List<MemberCheckInSearchResultViewModel> MatchingMembers { get; set; } = new();
        public List<AttendanceListItemViewModel> Records { get; set; } = new();

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
