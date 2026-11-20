using System.Threading.Tasks;
using GymCrudApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrudApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly IAdminDashboardService _dashboardService;

        public AdminDashboardController(IAdminDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // GET: /AdminDashboard
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = await _dashboardService.GetDashboardMetricsAsync();
            return View(vm);
        }

        // GET: /AdminDashboard/ChartData (JSON endpoint for Chart.js)
        [HttpGet]
        public async Task<IActionResult> ChartData()
        {
            var vm = await _dashboardService.GetDashboardMetricsAsync();
            return Json(new
            {
                dailyCheckInLabels = vm.DailyCheckInLabels,
                dailyCheckInCounts = vm.DailyCheckInCounts,
                monthlyRevenueLabels = vm.MonthlyRevenueLabels,
                monthlyRevenueTotals = vm.MonthlyRevenueTotals
            });
        }
    }
}