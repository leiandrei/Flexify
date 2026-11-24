using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GymCrudApp.ViewModels.ReceiptOcr;
using Microsoft.AspNetCore.Http;

namespace GymCrudApp.Services
{
    public interface IReceiptOcrService
    {
        Task<(bool Success, string? ErrorMessage, ReceiptConfirmViewModel? ConfirmModel)> ProcessReceiptUploadAsync(
            IFormFile receiptFile,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? ErrorMessage, ReceiptConfirmViewModel? ConfirmModel)> ProcessReceiptUploadAsync(
            IFormFile receiptFile,
            string? clientRawText,
            CancellationToken cancellationToken = default);

        Task PrepareConfirmationModelAsync(
            ReceiptConfirmViewModel model,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? ErrorMessage, int? PaymentId)> ConfirmAndSavePaymentAsync(
            ReceiptConfirmViewModel model,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default);
    }
}