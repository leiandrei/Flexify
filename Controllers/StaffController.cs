using System.Threading.Tasks;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrudApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StaffController : Controller
    {
        private readonly IStaffService _staffService;

        public StaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        // GET: /Staff
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var staffList = await _staffService.GetAllAsync();
            return View(staffList);
        }

        // GET: /Staff/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var staff = await _staffService.GetByIdAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            return View(staff);
        }

        // GET: /Staff/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new StaffCreateViewModel());
        }

        // POST: /Staff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, errorMessage) = await _staffService.CreateAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to create staff account.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Staff member '{model.FullName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Staff/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _staffService.GetForEditAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        // POST: /Staff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StaffEditViewModel model)
        {
            if (id != model.StaffID)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, errorMessage) = await _staffService.UpdateAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to update staff member.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Staff member '{model.FullName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Staff/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var staff = await _staffService.GetByIdAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            return View(staff);
        }

        // POST: /Staff/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (success, errorMessage) = await _staffService.DeleteAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage ?? "Unable to delete staff member.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Staff account deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}