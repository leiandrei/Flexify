using System.Threading.Tasks;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrudApp.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class MembersController : Controller
    {
        private readonly IMemberService _memberService;

        public MembersController(IMemberService memberService)
        {
            _memberService = memberService;
        }

        // GET: /Members
        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? status, string? plan, int page = 1)
        {
            var model = await _memberService.GetPagedAsync(searchTerm, status, plan, page, pageSize: 10);
            return View(model);
        }

        // GET: /Members/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var member = await _memberService.GetByIdAsync(id);
            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // GET: /Members/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new MemberCreateViewModel());
        }

        // POST: /Members/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MemberCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, errorMessage) = await _memberService.CreateAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to create member.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Member '{model.FullName}' registered successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Members/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _memberService.GetForEditAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        // POST: /Members/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MemberEditViewModel model)
        {
            if (id != model.MemberId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, errorMessage) = await _memberService.UpdateAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to update member.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Member '{model.FullName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Members/Delete/5 (Deactivation confirmation page)
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var member = await _memberService.GetByIdAsync(id);
            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // POST: /Members/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deactivated = await _memberService.DeactivateAsync(id);
            if (!deactivated)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Member account deactivated.";
            return RedirectToAction(nameof(Index));
        }
        
        // POST: /Members/PermanentDelete/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentDelete(int id)
        {
            var (success, errorMessage) = await _memberService.DeletePermanentlyAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage ?? "Unable to delete member.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Inactive member permanently deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}