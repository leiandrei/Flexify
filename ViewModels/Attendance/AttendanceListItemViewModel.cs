using System;

namespace GymCrudApp.ViewModels.Attendance
{
    public class AttendanceListItemViewModel
    {
        public int AttendanceId { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string PlanName { get; set; } = "—";
        public DateTime CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public bool IsOpen => !CheckOut.HasValue;

        public string DurationDisplay
        {
            get
            {
                if (!CheckOut.HasValue) return "In Gym";
                var span = CheckOut.Value - CheckIn;
                return $"{(int)span.TotalHours}h {span.Minutes}m";
            }
        }
    }
}