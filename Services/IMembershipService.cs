using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GymCrudApp.ViewModels.Memberships;

namespace GymCrudApp.Services
{
    public interface IMembershipService
    {
        DateTime CalculateEndDate(DateTime startDate, int durationDays);
        int CalculateDaysRemaining(DateTime endDate, DateTime? asOfDate = null);
        string ComputeStatus(DateTime endDate, bool isActive = true, DateTime? asOfDate = null);

        Task<List<MembershipListItemViewModel>> GetAllAsync();
        Task<MembershipDetailsViewModel?> GetByIdAsync(int id);
        Task<MembershipDetailsViewModel?> GetLatestForMemberAsync(int memberId);
        Task<MembershipCreateViewModel> BuildCreateViewModelAsync(int? preselectedMemberId = null);
        Task<MembershipEditViewModel?> GetForEditAsync(int id);
        Task<(bool Success, string? ErrorMessage)> CreateAsync(MembershipCreateViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(MembershipEditViewModel model);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id);
    }
}