using System.Threading.Tasks;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.MembershipPlans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrudApp.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class MembershipPlansController : Controller
    {
        private readonly IMembershipPlanService _planService;

        public MembershipPlansController(IMembershipPlanService planService)
        {
            _planService = planService;
        }

        // GET: /MembershipPlans
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var plans = await _planService.GetAllAsync();
            return View(plans);
        }

        // GET: /MembershipPlans/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var plan = await _planService.GetByIdAsync(id);
            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // GET: /MembershipPlans/Create
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new MembershipPlanFormViewModel());
        }

        // POST: /MembershipPlans/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MembershipPlanFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, errorMessage) = await _planService.CreateAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to create plan.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Plan '{model.PlanName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /MembershipPlans/Edit/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _planService.GetForEditAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        // POST: /MembershipPlans/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MembershipPlanFormViewModel model)
        {
            if (id != model.PlanId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, errorMessage) = await _planService.UpdateAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to update plan.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Plan '{model.PlanName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /MembershipPlans/Delete/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var plan = await _planService.GetByIdAsync(id);
            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // POST: /MembershipPlans/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (success, errorMessage) = await _planService.DeleteAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage ?? "Unable to delete plan.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Membership plan deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}