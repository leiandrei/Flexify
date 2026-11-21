using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GymCrudApp.ViewModels.ReceiptOcr;
using Microsoft.AspNetCore.Http;

namespace GymCrudApp.Services
{
    public interface IReceiptOcrService
    {
        (bool IsValid, string? ErrorMessage) ValidateUpload(IFormFile? file);
        ParsedReceiptCandidates ParseReceiptText(string rawText);
        Task<(bool Success, string? ErrorMessage, ReceiptConfirmViewModel? Model)> ProcessReceiptUploadAsync(
            IFormFile file,
            CancellationToken cancellationToken = default);
        Task<ReceiptConfirmViewModel> PrepareConfirmationModelAsync(
            ReceiptConfirmViewModel model,
            CancellationToken cancellationToken = default);
        Task<(bool Success, string? ErrorMessage, int? PaymentId)> ConfirmAndSavePaymentAsync(
            ReceiptConfirmViewModel model,
            ClaimsPrincipal staffUser,
            CancellationToken cancellationToken = default);
    }
}