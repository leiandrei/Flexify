using System.Threading.Tasks;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.Memberships;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrudApp.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class MembershipsController : Controller
    {
        private readonly IMembershipService _membershipService;

        public MembershipsController(IMembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var items = await _membershipService.GetAllAsync();
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _membershipService.GetByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? memberId)
        {
            var vm = await _membershipService.BuildCreateViewModelAsync(memberId);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MembershipCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var reloaded = await _membershipService.BuildCreateViewModelAsync(model.MemberId);
                model.Members = reloaded.Members;
                model.Plans = reloaded.Plans;
                return View(model);
            }

            var (success, errorMessage) = await _membershipService.CreateAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to assign membership.");
                var reloaded = await _membershipService.BuildCreateViewModelAsync(model.MemberId);
                model.Members = reloaded.Members;
                model.Plans = reloaded.Plans;
                return View(model);
            }

            TempData["SuccessMessage"] = "Membership assigned successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _membershipService.GetForEditAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MembershipEditViewModel model)
        {
            if (id != model.MembershipId) return BadRequest();

            if (!ModelState.IsValid)
            {
                var reloaded = await _membershipService.GetForEditAsync(id);
                model.Members = reloaded?.Members;
                model.Plans = reloaded?.Plans;
                return View(model);
            }

            var (success, errorMessage) = await _membershipService.UpdateAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to update membership.");
                var reloaded = await _membershipService.GetForEditAsync(id);
                model.Members = reloaded?.Members;
                model.Plans = reloaded?.Plans;
                return View(model);
            }

            TempData["SuccessMessage"] = "Membership updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _membershipService.GetByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (success, errorMessage) = await _membershipService.DeleteAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage ?? "Unable to delete membership.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Membership deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}