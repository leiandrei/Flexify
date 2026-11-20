using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GymCrudApp.ViewModels.Renewals;

namespace GymCrudApp.Services
{
    public interface IRenewalService
    {
        Task<List<ExpiringMemberItemViewModel>> GetExpiringSoonMembersAsync(
            int withinDays = 7,
            bool includeRecentlyExpired = true,
            DateTime? asOfDate = null);

        Task<OneClickRenewalViewModel?> PrepareRenewalFormAsync(
            int memberId,
            int? overridePlanId = null,
            DateTime? asOfDate = null);

        Task<RenewalResult> RenewMembershipAsync(
            OneClickRenewalViewModel model,
            int recordedByStaffId,
            DateTime? asOfDate = null);

        Task<int> DeactivateExpiredMembershipsAsync(DateTime? asOfDate = null);
    }
}