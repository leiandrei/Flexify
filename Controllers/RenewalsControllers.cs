using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.Renewals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class RenewalsController : Controller
{
    private readonly IRenewalService _renewalService;
    private readonly GymDbContext _context;

    public RenewalsController(IRenewalService renewalService, GymDbContext context)
    {
        _renewalService = renewalService;
        _context = context;
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
        if (vm == null)
        {
            return NotFound();
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Renew(OneClickRenewalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var refreshed = await _renewalService.PrepareRenewalFormAsync(model.MemberId, model.PlanId);
            if (refreshed != null)
            {
                model.AvailablePlans = refreshed.AvailablePlans;
            }

            return View(model);
        }

        var staffId = await ResolveCurrentStaffIdAsync();
        var result = await _renewalService.RenewMembershipAsync(model, staffId);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to complete renewal.");
            var refreshed = await _renewalService.PrepareRenewalFormAsync(model.MemberId, model.PlanId);
            if (refreshed != null)
            {
                model.AvailablePlans = refreshed.AvailablePlans;
            }

            return View(model);
        }

        TempData["SuccessMessage"] = $"Renewed {model.MemberName}'s membership through {result.NewEndDate:MMM dd, yyyy}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int> ResolveCurrentStaffIdAsync()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(rawUserId, out var userId))
        {
            var staff = await _context.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.userId == userId);

            if (staff != null)
            {
                return staff.StaffID;
            }
        }

        var fallbackStaffId = await _context.Staff
            .AsNoTracking()
            .Select(s => s.StaffID)
            .FirstOrDefaultAsync();

        return fallbackStaffId > 0 ? fallbackStaffId : 1;
    }
}