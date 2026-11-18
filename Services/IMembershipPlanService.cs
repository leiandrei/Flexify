using System.Collections.Generic;
using System.Threading.Tasks;
using GymCrudApp.ViewModels.MembershipPlans;

namespace GymCrudApp.Services
{
    public interface IMembershipPlanService
    {
        Task<List<MembershipPlanViewModel>> GetAllAsync();
        Task<MembershipPlanViewModel?> GetByIdAsync(int id);
        Task<MembershipPlanFormViewModel?> GetForEditAsync(int id);
        Task<(bool Success, string? ErrorMessage)> CreateAsync(MembershipPlanFormViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(MembershipPlanFormViewModel model);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id);
    }
}