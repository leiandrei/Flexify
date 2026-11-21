using System.Threading;
using System.Threading.Tasks;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.ReceiptOcr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrudApp.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class ReceiptOcrController : Controller
    {
        private readonly IReceiptOcrService _receiptOcrService;

        public ReceiptOcrController(IReceiptOcrService receiptOcrService)
        {
            _receiptOcrService = receiptOcrService;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View(new ReceiptUploadViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ReceiptUploadViewModel model, CancellationToken cancellationToken)
        {
            if (model.ReceiptFile == null)
            {
                ModelState.AddModelError(nameof(model.ReceiptFile), "Please select a receipt file.");
                return View(model);
            }

            var (success, errorMessage, confirmModel) =
                await _receiptOcrService.ProcessReceiptUploadAsync(model.ReceiptFile, cancellationToken);

            if (!success || confirmModel == null)
            {
                ModelState.AddModelError(nameof(model.ReceiptFile), errorMessage ?? "Unable to process receipt.");
                return View(model);
            }

            return View("Confirm", confirmModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(ReceiptConfirmViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await _receiptOcrService.PrepareConfirmationModelAsync(model, cancellationToken);
                return View(model);
            }

            var (success, errorMessage, paymentId) =
                await _receiptOcrService.ConfirmAndSavePaymentAsync(model, User, cancellationToken);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to save payment.");
                await _receiptOcrService.PrepareConfirmationModelAsync(model, cancellationToken);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Receipt payment #{paymentId} confirmed and saved.";
            return RedirectToAction("Index", "Payments");
        }
    }
}