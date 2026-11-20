using System.Threading.Tasks;
using GymCrudApp.ViewModels.Home;

namespace GymCrudApp.Services
{
    public interface ILandingPageService
    {
        Task<LandingPageViewModel> GetLandingPageDataAsync();
    }
}