using System;

namespace GymCrudApp.ViewModels.Staff
{
    public class StaffListItemViewModel
    {
        public int StaffID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public bool IsActive { get; set; }
    }
}