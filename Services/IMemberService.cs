using System.Threading.Tasks;
using GymCrudApp.ViewModels.Members;

namespace GymCrudApp.Services
{
    public interface IMemberService
    {
        Task<MemberIndexViewModel> GetPagedAsync(string? searchTerm, int page, int pageSize = 10);
        Task<MemberDetailsViewModel?> GetByIdAsync(int id);
        Task<MemberEditViewModel?> GetForEditAsync(int id);
        Task<(bool Success, string? ErrorMessage)> CreateAsync(MemberCreateViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(MemberEditViewModel model);
        Task<(bool Success, string? ErrorMessage)> DeletePermanentlyAsync(int id);
        Task<bool> DeactivateAsync(int id);
    }
}