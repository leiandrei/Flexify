using System;
using System.Threading.Tasks;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrudApp.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // GET: /Payments
        [HttpGet]
        public async Task<IActionResult> Index(
            string? memberSearch,
            DateTime? filterDate,
            string? paymentType,
            int? planId)
        {
            var vm = await _paymentService.GetFilteredAsync(memberSearch, filterDate, paymentType, planId);
            return View(vm);
        }

        // GET: /Payments/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _paymentService.GetByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // GET: /Payments/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? memberId)
        {
            var vm = await _paymentService.BuildCreateViewModelAsync(memberId, User.Identity?.Name);
            return View(vm);
        }

        // POST: /Payments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var reloaded = await _paymentService.BuildCreateViewModelAsync(model.MemberId, User.Identity?.Name);
                model.Members = reloaded.Members;
                model.Plans = reloaded.Plans;
                model.StaffList = reloaded.StaffList;
                model.PlanChoices = reloaded.PlanChoices;
                return View(model);
            }

            var (success, errorMessage) = await _paymentService.CreateAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to record payment.");
                var reloaded = await _paymentService.BuildCreateViewModelAsync(model.MemberId, User.Identity?.Name);
                model.Members = reloaded.Members;
                model.Plans = reloaded.Plans;
                model.StaffList = reloaded.StaffList;
                model.PlanChoices = reloaded.PlanChoices;
                return View(model);
            }

            TempData["SuccessMessage"] = "Membership assigned and payment recorded in a single transaction.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Payments/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _paymentService.GetForEditAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // POST: /Payments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PaymentEditViewModel model)
        {
            if (id != model.PaymentId) return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, errorMessage) = await _paymentService.UpdateAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to update payment.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Payment updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Payments/Void/5 (Void confirmation page)
        [HttpGet]
        public async Task<IActionResult> Void(int id)
        {
            var item = await _paymentService.GetByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: /Payments/Void/5
        [HttpPost, ActionName("Void")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VoidConfirmed(int id)
        {
            var (success, errorMessage) = await _paymentService.VoidAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage ?? "Unable to void payment.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Payment has been voided.";
            return RedirectToAction(nameof(Index));
        }
    }
}