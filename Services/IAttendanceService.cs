using System;
using System.Threading.Tasks;
using GymCrudApp.ViewModels.Attendance;

namespace GymCrudApp.Services
{
    public interface IAttendanceService
    {
        Task<AttendanceIndexViewModel> GetIndexViewModelAsync(
            string? searchQuery,
            DateTime? filterDate,
            int page = 1,
            int pageSize = 10,
            DateTime? asOfNow = null);

        Task<(bool Success, string? WarningMessage, string? ErrorMessage)> CheckInAsync(
            int memberId,
            DateTime? checkInTime = null);

        Task<(bool Success, string? ErrorMessage)> CheckOutAsync(
            int attendanceId,
            DateTime? checkOutTime = null);

        Task<AttendanceListItemViewModel?> GetByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id);
    }
}