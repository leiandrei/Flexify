using System;
using System.Threading.Tasks;
using GymCrudApp.ViewModels.AdminDashboard;

namespace GymCrudApp.Services
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardViewModel> GetDashboardMetricsAsync(DateTime? asOfDate = null);
    }
}