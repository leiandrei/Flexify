using System;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly GymDbContext _context;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            GymDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Prevent duplicate emails across both Identity and custom Users table
            if (await _userManager.FindByEmailAsync(model.Email) != null ||
                await _context.Users.AnyAsync(u => u.userEmail == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                return View(model);
            }

            var identityUser = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(identityUser, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            try
            {
                // 1. Assign the Member role in ASP.NET Identity
                await _userManager.AddToRoleAsync(identityUser, "Member");

                // 2. Create linked Users and Members rows in a single DB transaction
                await using var transaction = await _context.Database.BeginTransactionAsync();

                var appUser = new Users
                {
                    userEmail = model.Email,
                    passwordHash = identityUser.PasswordHash ?? "MANAGED_BY_IDENTITY",
                    role = "Member",
                    isActive = true
                };

                _context.Users.Add(appUser);
                await _context.SaveChangesAsync();

                var member = new Members
                {
                    userId = appUser.userId,
                    fullName = model.FullName,
                    contactNumber = model.ContactNumber,
                    birthdate = model.Birthdate,
                    userEmail = model.Email,
                    joinDate = DateTime.Today,
                    isActive = true
                };

                _context.Members.Add(member);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Sign in the newly registered member and redirect to Member destination
                await _signInManager.SignInAsync(identityUser, isPersistent: false);
                return await RedirectByRoleAsync(identityUser);
            }
            catch
            {
                // Roll back the Identity account if creating Users/Members rows fails
                await _userManager.DeleteAsync(identityUser);
                ModelState.AddModelError(string.Empty, "Registration failed while saving member profile. Please try again.");
                return View(model);
            }
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // lockoutOnFailure: true increments failed attempts and locks after MaxFailedAccessAttempts
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    return await RedirectByRoleAsync(user);
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account locked due to multiple failed login attempts. Try again in 5 minutes.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task<IActionResult> RedirectByRoleAsync(IdentityUser user)
        {
            // Update these controller targets as Admin Dashboard, Members CRUD, and Member Portal are built
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Home");
            }

            if (await _userManager.IsInRoleAsync(user, "Staff"))
            {
                return RedirectToAction("Index", "Home");
            }

            if (await _userManager.IsInRoleAsync(user, "Member"))
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}