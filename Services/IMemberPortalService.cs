using System;
using System.Threading.Tasks;
using GymCrudApp.ViewModels.MemberPortal;

namespace GymCrudApp.Services
{
    public interface IMemberPortalService
    {
        Task<MemberPortalViewModel?> GetPortalForUserAsync(
            int? authenticatedUserId,
            string? authenticatedEmail,
            DateTime? asOfDate = null);

        Task<(bool Authorized, MemberPortalViewModel? Data)> GetPortalForMemberIdAsync(
            int requestedMemberId,
            int? authenticatedUserId,
            string? authenticatedEmail,
            DateTime? asOfDate = null);
    }
}