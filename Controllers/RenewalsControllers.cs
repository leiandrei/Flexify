using System.Security.Claims;
using System.Threading.Tasks;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.Renewals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrudApp.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class RenewalsController : Controller
    {
        private readonly IRenewalService _renewalService;

        public RenewalsController(IRenewalService renewalService)
        {
            _renewalService = renewalService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var expiringMembers = await _renewalService.GetExpiringSoonMembersAsync(
                withinDays: 7,
                includeRecentlyExpired: true);

            return View(expiringMembers);
        }

        [HttpGet]
        public async Task<IActionResult> Renew(int id, int? planId = null)
        {
            var vm = await _renewalService.PrepareRenewalFormAsync(id, planId);
            if (vm == null) return NotFound();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Renew(OneClickRenewalViewModel model)
        {
            if (!ModelState.IsValid)
                return await RedisplayAsync(model);

            var staffId = await ResolveCurrentStaffIdAsync();
            if (staffId is null)
            {
                ModelState.AddModelError(string.Empty, "Your account isn't linked to a staff profile.");
                return await RedisplayAsync(model);
            }

            var result = await _renewalService.RenewMembershipAsync(model, staffId.Value);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to complete renewal.");
                return await RedisplayAsync(model);
            }

            TempData["SuccessMessage"] = $"Renewed {result.MemberName}'s membership through {result.NewEndDate:MMM dd, yyyy}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> RedisplayAsync(OneClickRenewalViewModel model)
        {
            var refreshed = await _renewalService.PrepareRenewalFormAsync(model.MemberId, model.PlanId);
            if (refreshed == null) return NotFound();

            model.AvailablePlans = refreshed.AvailablePlans;
            model.MemberName = refreshed.MemberName;
            model.CurrentPlanName = refreshed.CurrentPlanName;
            model.CurrentEndDate = refreshed.CurrentEndDate;
            model.CurrentStatus = refreshed.CurrentStatus;
            model.ProjectedStartDate = refreshed.ProjectedStartDate;
            model.ProjectedEndDate = refreshed.ProjectedEndDate;

            return View(model);
        }

        private async Task<int?> ResolveCurrentStaffIdAsync()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return null;

            return await _renewalService.GetStaffIdByUserIdAsync(userId);
        }
    }
}