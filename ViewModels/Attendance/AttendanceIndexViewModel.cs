using System;
using System.Collections.Generic;

namespace GymCrudApp.ViewModels.Attendance
{
    public class AttendanceIndexViewModel
    {
        public string? SearchQuery { get; set; }
        public DateTime FilterDate { get; set; } = DateTime.Today;
        public List<MemberCheckInSearchResultViewModel> MatchingMembers { get; set; } = new();
        public List<AttendanceListItemViewModel> Records { get; set; } = new();
    }
}