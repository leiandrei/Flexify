using System;
using System.Linq;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.ViewModels.Members;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Services
{
    public class MemberService : IMemberService
    {
        private readonly GymDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MemberService(GymDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<MemberIndexViewModel> GetPagedAsync(
            string? searchTerm,
            string? status,
            string? plan,
            int page,
            int pageSize = 10)
        {
            if (page < 1) page = 1;

            var query = _context.Members.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(m =>
                    m.fullName.Contains(term) ||
                    m.userEmail.Contains(term) ||
                    m.contactNumber.Contains(term));
            }

            // Status/plan need membership context, so enrich then filter, then page.
            var items = await query
                .OrderByDescending(m => m.isActive)
                .ThenBy(m => m.fullName)
                .Select(m => new MemberListItemViewModel
                {
                    MemberId = m.memberId,
                    FullName = m.fullName,
                    Email = m.userEmail,
                    ContactNumber = m.contactNumber,
                    JoinDate = m.joinDate,
                    IsActive = m.isActive
                })
                .ToListAsync();

            var memberIds = items.Select(i => i.MemberId).ToList();
            if (memberIds.Count > 0)
            {
                var today = DateTime.Today;
                var memberships = await _context.Memberships
                    .AsNoTracking()
                    .Include(ms => ms.Plan)
                    .Where(ms => memberIds.Contains(ms.memberId))
                    .ToListAsync();

                foreach (var item in items)
                {
                    var latest = memberships
                        .Where(ms => ms.memberId == item.MemberId)
                        .OrderByDescending(ms => ms.isActive)
                        .ThenByDescending(ms => ms.endDate)
                        .FirstOrDefault();

                    if (latest == null)
                    {
                        // Account off → Inactive; account on with no subscription → No plan
                        item.MembershipStatus = item.IsActive ? "No plan" : "Inactive";
                        continue;
                    }

                    var diff = (latest.endDate.Date - today).Days;
                    item.PlanName = latest.Plan?.planName;
                    item.MembershipEndDate = latest.endDate;
                    item.DaysRemaining = diff < 0 ? 0 : diff;
                    item.MembershipStatus = !latest.isActive ? "Inactive"
                        : diff < 0 ? "Expired"
                        : diff <= 7 ? "Expiring"
                        : "Active";
                }
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim();
                items = items.Where(i => i.MembershipStatus == st).ToList();
            }

            if (!string.IsNullOrWhiteSpace(plan))
            {
                var planFilter = plan.Trim();
                if (planFilter == "No plan")
                {
                    items = items.Where(i => string.IsNullOrWhiteSpace(i.PlanName)).ToList();
                }
                else
                {
                    items = items.Where(i => i.PlanName == planFilter).ToList();
                }
            }

            var totalCount = items.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var pageItems = items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var planChoices = await _context.MembershipPlans
                .AsNoTracking()
                .Where(p => p.isActive)
                .OrderBy(p => p.planPrice)
                .Select(p => p.planName)
                .ToListAsync();

            return new MemberIndexViewModel
            {
                Members = pageItems,
                SearchTerm = searchTerm,
                Status = status,
                Plan = plan,
                PlanChoices = planChoices.Select(name => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = name,
                    Text = name,
                    Selected = plan == name
                }),
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<MemberDetailsViewModel?> GetByIdAsync(int id)
        {
            var member = await _context.Members
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.memberId == id);

            if (member == null) return null;

            var latest = await _context.Memberships
                .AsNoTracking()
                .Include(ms => ms.Plan)
                .Where(ms => ms.memberId == id)
                .OrderByDescending(ms => ms.isActive)
                .ThenByDescending(ms => ms.endDate)
                .FirstOrDefaultAsync();

            var today = DateTime.Today;
            string status = "No Membership";
            int? daysRemaining = null;

            if (latest != null)
            {
                var diff = (latest.endDate.Date - today).Days;
                daysRemaining = diff < 0 ? 0 : diff;
                status = !latest.isActive ? "Inactive"
                    : diff < 0 ? "Expired"
                    : diff <= 7 ? "Expiring"
                    : "Active";
            }

            var payments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.memberId == id)
                .OrderByDescending(p => p.paymentDate)
                .ThenByDescending(p => p.paymentId)
                .Take(20)
                .Select(p => new MemberPaymentRowViewModel
                {
                    PaymentId = p.paymentId,
                    PaymentDate = p.paymentDate,
                    PlanName = p.Membership != null && p.Membership.Plan != null
                        ? p.Membership.Plan.planName
                        : "—",
                    PaymentType = p.paymentType,
                    ReferenceNumber = p.referenceNumber,
                    RecordedByStaffName = p.RecordedByStaff != null
                        ? p.RecordedByStaff.fullName
                        : "—",
                    Amount = p.amount,
                    IsVoided = p.isVoided
                })
                .ToListAsync();

            var visits = await _context.Attendance
                .AsNoTracking()
                .Where(a => a.memberId == id)
                .OrderByDescending(a => a.checkIn)
                .Take(20)
                .Select(a => new MemberVisitRowViewModel
                {
                    CheckIn = a.checkIn,
                    CheckOut = a.checkOut
                })
                .ToListAsync();

            return new MemberDetailsViewModel
            {
                MemberId = member.memberId,
                UserId = member.userId,
                FullName = member.fullName,
                Email = member.userEmail,
                ContactNumber = member.contactNumber,
                Birthdate = member.birthdate,
                JoinDate = member.joinDate,
                IsActive = member.isActive,
                CurrentPlanId = latest?.planId,
                CurrentPlanName = latest?.Plan?.planName,
                CurrentPlanPrice = latest?.Plan?.planPrice,
                PlanDurationDays = latest?.Plan?.durationDays,
                MembershipStartDate = latest?.startDate,
                MembershipEndDate = latest?.endDate,
                DaysRemaining = daysRemaining,
                MembershipStatus = status,
                Payments = payments,
                Visits = visits
            };
        }

        public async Task<MemberEditViewModel?> GetForEditAsync(int id)
        {
            return await _context.Members
                .AsNoTracking()
                .Where(m => m.memberId == id)
                .Select(m => new MemberEditViewModel
                {
                    MemberId = m.memberId,
                    FullName = m.fullName,
                    Email = m.userEmail,
                    ContactNumber = m.contactNumber,
                    Birthdate = m.birthdate,
                    IsActive = m.isActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateAsync(MemberCreateViewModel model)
        {
            if (await _userManager.FindByEmailAsync(model.Email) != null ||
                await _context.Users.AnyAsync(u => u.userEmail == model.Email))
            {
                return (false, "An account with this email already exists.");
            }

            var identityUser = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
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
                await _userManager.AddToRoleAsync(identityUser, "Member");

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
                return (true, null);
            }
            catch
            {
                await _userManager.DeleteAsync(identityUser);
                return (false, "Failed to save member profile. Please try again.");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(MemberEditViewModel model)
        {
            var member = await _context.Members.FirstOrDefaultAsync(m => m.memberId == model.MemberId);
            if (member == null)
            {
                return (false, "Member not found.");
            }

            var emailInUse = await _context.Users
                .AnyAsync(u => u.userEmail == model.Email && u.userId != member.userId);
            if (emailInUse)
            {
                return (false, "Another account is already using this email address.");
            }

            var oldEmail = member.userEmail;
            member.fullName = model.FullName;
            member.contactNumber = model.ContactNumber;
            member.birthdate = model.Birthdate;
            member.userEmail = model.Email;
            member.isActive = model.IsActive;

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.userId == member.userId);
            if (appUser != null)
            {
                appUser.userEmail = model.Email;
                appUser.isActive = model.IsActive;
            }

            var identityUser = await _userManager.FindByEmailAsync(oldEmail);
            if (identityUser != null)
            {
                if (!string.Equals(oldEmail, model.Email, StringComparison.OrdinalIgnoreCase))
                {
                    identityUser.Email = model.Email;
                    identityUser.UserName = model.Email;
                }
                identityUser.LockoutEnd = model.IsActive ? null : DateTimeOffset.MaxValue;
                await _userManager.UpdateAsync(identityUser);
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var member = await _context.Members.FirstOrDefaultAsync(m => m.memberId == id);
            if (member == null)
            {
                return false;
            }

            member.isActive = false;

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.userId == member.userId);
            if (appUser != null)
            {
                appUser.isActive = false;
            }

            var identityUser = await _userManager.FindByEmailAsync(member.userEmail);
            if (identityUser != null)
            {
                identityUser.LockoutEnd = DateTimeOffset.MaxValue;
                await _userManager.UpdateAsync(identityUser);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<(bool Success, string? ErrorMessage)> DeletePermanentlyAsync(int id)
        {
            var member = await _context.Members.FirstOrDefaultAsync(m => m.memberId == id);
            if (member == null)
            {
                return (false, "Member not found.");
            }

            if (member.isActive)
            {
                return (false, "Deactivate this member before permanently deleting their record.");
            }

            var hasRecords =
                await _context.Memberships.AnyAsync(ms => ms.memberId == id) ||
                await _context.Payments.AnyAsync(p => p.memberId == id) ||
                await _context.Attendance.AnyAsync(a => a.memberId == id);

            if (hasRecords)
            {
                return (false, "Cannot permanently delete this member because they have existing memberships, payments, or attendance history.");
            }

            var identityUser = await _userManager.FindByEmailAsync(member.userEmail);
            if (identityUser != null)
            {
                var roles = await _userManager.GetRolesAsync(identityUser);
                if (roles.Count > 0)
                {
                    var removeRolesResult = await _userManager.RemoveFromRolesAsync(identityUser, roles);
                    if (!removeRolesResult.Succeeded)
                    {
                        var roleErrors = string.Join(" ", removeRolesResult.Errors.Select(e => e.Description));
                        return (false, $"Failed to remove member roles: {roleErrors}");
                    }
                }

                var deleteUserResult = await _userManager.DeleteAsync(identityUser);
                if (!deleteUserResult.Succeeded)
                {
                    var deleteErrors = string.Join(" ", deleteUserResult.Errors.Select(e => e.Description));
                    return (false, $"Failed to delete login account: {deleteErrors}");
                }
            }

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.userId == member.userId);

            _context.Members.Remove(member);
            if (appUser != null)
            {
                _context.Users.Remove(appUser);
            }

            await _context.SaveChangesAsync();

            return (true, null);
        }
    }
    
    
}