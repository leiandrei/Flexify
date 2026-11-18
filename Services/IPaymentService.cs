using System;
using System.Threading.Tasks;
using GymCrudApp.ViewModels.Payments;

namespace GymCrudApp.Services
{
    public interface IPaymentService
    {
        Task<PaymentIndexViewModel> GetFilteredAsync(string? memberSearch, DateTime? filterDate);
        Task<PaymentDetailsViewModel?> GetByIdAsync(int id);
        Task<PaymentCreateViewModel> BuildCreateViewModelAsync(int? preselectedMemberId = null, string? currentUserEmail = null);
        Task<PaymentEditViewModel?> GetForEditAsync(int id);
        Task<(bool Success, string? ErrorMessage)> CreateAsync(PaymentCreateViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(PaymentEditViewModel model);
        Task<(bool Success, string? ErrorMessage)> VoidAsync(int id);
    }
}