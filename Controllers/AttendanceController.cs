using System;
using System.Threading.Tasks;
using GymCrudApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrudApp.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        // GET: /Attendance
        [HttpGet]
        public async Task<IActionResult> Index(string? searchQuery, DateTime? filterDate)
        {
            var vm = await _attendanceService.GetIndexViewModelAsync(searchQuery, filterDate);
            return View(vm);
        }

        // POST: /Attendance/CheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int memberId, string? searchQuery)
        {
            var (success, warningMessage, errorMessage) = await _attendanceService.CheckInAsync(memberId);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage ?? "Unable to check in member.";
            }
            else
            {
                TempData["SuccessMessage"] = "Member checked in successfully.";
                if (!string.IsNullOrWhiteSpace(warningMessage))
                {
                    TempData["WarningMessage"] = warningMessage;
                }
            }

            return RedirectToAction(nameof(Index), new { searchQuery });
        }

        // POST: /Attendance/CheckOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int attendanceId, DateTime? filterDate)
        {
            var (success, errorMessage) = await _attendanceService.CheckOutAsync(attendanceId);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage ?? "Unable to check out member.";
            }
            else
            {
                TempData["SuccessMessage"] = "Member checked out successfully.";
            }

            return RedirectToAction(nameof(Index), new { filterDate = filterDate?.ToString("yyyy-MM-dd") });
        }

        // GET: /Attendance/Delete/5 (Admin Only)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _attendanceService.GetByIdAsync(id);
            if (record == null) return NotFound();
            return View(record);
        }

        // POST: /Attendance/Delete/5 (Admin Only)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (success, errorMessage) = await _attendanceService.DeleteAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage ?? "Unable to delete attendance record.";
            }
            else
            {
                TempData["SuccessMessage"] = "Attendance record deleted.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}