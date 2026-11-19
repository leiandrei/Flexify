using System.Collections.Generic;
using System.Threading.Tasks;
using GymCrudApp.ViewModels.Staff;

namespace GymCrudApp.Services
{
    public interface IStaffService
    {
        Task<List<StaffListItemViewModel>> GetAllAsync();
        Task<StaffDetailsViewModel?> GetByIdAsync(int id);
        Task<StaffEditViewModel?> GetForEditAsync(int id);
        Task<(bool Success, string? ErrorMessage)> CreateAsync(StaffCreateViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(StaffEditViewModel model);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id);
    }
}