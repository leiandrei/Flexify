using System.Security.Claims;
using System.Threading.Tasks;
using GymCrudApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrudApp.Controllers
{
    [Authorize(Roles = "Member")]
    public class MemberPortalController : Controller
    {
        private readonly IMemberPortalService _memberPortalService;

        public MemberPortalController(IMemberPortalService memberPortalService)
        {
            _memberPortalService = memberPortalService;
        }

        // GET: /MemberPortal
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var (userId, email) = ExtractUserClaims();
            var vm = await _memberPortalService.GetPortalForUserAsync(userId, email);
            if (vm == null) return Forbid();

            return View(vm);
        }

        // GET: /MemberPortal/Details/5 (Enforces cross-member isolation)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var (userId, email) = ExtractUserClaims();
            var (authorized, vm) = await _memberPortalService.GetPortalForMemberIdAsync(id, userId, email);
            if (!authorized || vm == null)
            {
                return Forbid();
            }

            return View("Index", vm);
        }

        private (int? UserId, string? Email) ExtractUserClaims()
        {
            int? parsedUserId = null;
            var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(rawId, out var id))
            {
                parsedUserId = id;
            }

            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            return (parsedUserId, email);
        }
    }
}