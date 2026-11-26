using System.Diagnostics;
using System.Threading.Tasks;
using GymCrudApp.Models;
using GymCrudApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrudApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILandingPageService _landingPageService;

        public HomeController(ILandingPageService landingPageService)
        {
            _landingPageService = landingPageService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var vm = await _landingPageService.GetLandingPageDataAsync();
            return View(vm);
        }

        // No [AllowAnonymous] here: protected by FallbackPolicy
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
