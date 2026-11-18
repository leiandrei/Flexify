using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.ViewModels.Staff;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Services
{
    public class StaffService : IStaffService
    {
        private readonly GymDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StaffService(GymDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<StaffListItemViewModel>> GetAllAsync()
        {
            return await _context.Staff
                .AsNoTracking()
                .Include(s => s.User)
                .OrderByDescending(s => s.User != null && s.User.isActive)
                .ThenBy(s => s.fullName)
                .Select(s => new StaffListItemViewModel
                {
                    StaffID = s.StaffID,
                    FullName = s.fullName,
                    Email = s.User != null ? s.User.userEmail : string.Empty,
                    Position = s.position,
                    DateOfBirth = s.dateOfBirth,
                    IsActive = s.User != null && s.User.isActive
                })
                .ToListAsync();
        }

        public async Task<StaffDetailsViewModel?> GetByIdAsync(int id)
        {
            return await _context.Staff
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.StaffID == id)
                .Select(s => new StaffDetailsViewModel
                {
                    StaffID = s.StaffID,
                    UserId = s.userId,
                    FullName = s.fullName,
                    Email = s.User != null ? s.User.userEmail : string.Empty,
                    Position = s.position,
                    DateOfBirth = s.dateOfBirth,
                    IsActive = s.User != null && s.User.isActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<StaffEditViewModel?> GetForEditAsync(int id)
        {
            return await _context.Staff
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.StaffID == id)
                .Select(s => new StaffEditViewModel
                {
                    StaffID = s.StaffID,
                    FullName = s.fullName,
                    Email = s.User != null ? s.User.userEmail : string.Empty,
                    Position = s.position,
                    DateOfBirth = s.dateOfBirth,
                    IsActive = s.User != null && s.User.isActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateAsync(StaffCreateViewModel model)
        {
            var trimmedEmail = model.Email.Trim();

            if (await _userManager.FindByEmailAsync(trimmedEmail) != null ||
                await _context.Users.AnyAsync(u => u.userEmail == trimmedEmail))
            {
                return (false, "An account with this email already exists.");
            }

            var identityUser = new IdentityUser
            {
                UserName = trimmedEmail,
                Email = trimmedEmail,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(identityUser, model.InitialPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
                return (false, errors);
            }

            try
            {
                await _userManager.AddToRoleAsync(identityUser, "Staff");

                await using var transaction = await _context.Database.BeginTransactionAsync();

                var appUser = new Users
                {
                    userEmail = trimmedEmail,
                    passwordHash = identityUser.PasswordHash ?? "MANAGED_BY_IDENTITY",
                    role = "Staff",
                    isActive = true
                };

                _context.Users.Add(appUser);
                await _context.SaveChangesAsync();

                var staff = new Staff
                {
                    userId = appUser.userId,
                    fullName = model.FullName.Trim(),
                    position = model.Position.Trim(),
                    dateOfBirth = model.DateOfBirth
                };

                _context.Staff.Add(staff);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return (true, null);
            }
            catch
            {
                await _userManager.DeleteAsync(identityUser);
                return (false, "Failed to save staff profile. Please try again.");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(StaffEditViewModel model)
        {
            var staff = await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffID == model.StaffID);

            if (staff == null)
            {
                return (false, "Staff member not found.");
            }

            var trimmedEmail = model.Email.Trim();
            var emailInUse = await _context.Users
                .AnyAsync(u => u.userEmail == trimmedEmail && u.userId != staff.userId);

            if (emailInUse)
            {
                return (false, "Another account is already using this email address.");
            }

            var oldEmail = staff.User?.userEmail ?? string.Empty;

            staff.fullName = model.FullName.Trim();
            staff.position = model.Position.Trim();
            staff.dateOfBirth = model.DateOfBirth;

            if (staff.User != null)
            {
                staff.User.userEmail = trimmedEmail;
                staff.User.isActive = model.IsActive;
            }

            if (!string.IsNullOrEmpty(oldEmail))
            {
                var identityUser = await _userManager.FindByEmailAsync(oldEmail);
                if (identityUser != null)
                {
                    if (!string.Equals(oldEmail, trimmedEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        identityUser.Email = trimmedEmail;
                        identityUser.UserName = trimmedEmail;
                    }
                    identityUser.LockoutEnd = model.IsActive ? null : DateTimeOffset.MaxValue;
                    await _userManager.UpdateAsync(identityUser);
                }
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id)
        {
            var staff = await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffID == id);

            if (staff == null)
            {
                return (false, "Staff member not found.");
            }

            var email = staff.User?.userEmail;
            var linkedUser = staff.User;

            try
            {
                _context.Staff.Remove(staff);
                if (linkedUser != null)
                {
                    _context.Users.Remove(linkedUser);
                }

                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(email))
                {
                    var identityUser = await _userManager.FindByEmailAsync(email);
                    if (identityUser != null)
                    {
                        await _userManager.DeleteAsync(identityUser);
                    }
                }

                return (true, null);
            }
            catch (DbUpdateException)
            {
                return (false, "Cannot delete this staff member because they have linked payment or attendance records. Uncheck 'Active Account' in Edit to revoke login access instead.");
            }
        }
    }
}