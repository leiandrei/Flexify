using System;
using System.Linq;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.ViewModels.Payments;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly GymDbContext _context;

        public PaymentService(GymDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentIndexViewModel> GetFilteredAsync(
            string? memberSearch,
            DateTime? filterDate,
            string? paymentType = null,
            int? planId = null)
        {
            var query = _context.Payments
                .AsNoTracking()
                .Include(p => p.Member)
                .Include(p => p.Membership!)
                    .ThenInclude(m => m.Plan)
                .Include(p => p.RecordedByStaff)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(memberSearch))
            {
                var term = memberSearch.Trim();
                query = query.Where(p =>
                    p.Member != null &&
                    (p.Member.fullName.Contains(term) || p.Member.userEmail.Contains(term)));
            }

            if (filterDate.HasValue)
            {
                var targetDate = filterDate.Value.Date;
                query = query.Where(p => p.paymentDate.Date == targetDate);
            }

            if (!string.IsNullOrWhiteSpace(paymentType))
            {
                var method = paymentType.Trim();
                query = query.Where(p => p.paymentType == method);
            }

            if (planId.HasValue)
            {
                query = query.Where(p =>
                    p.Membership != null && p.Membership.planId == planId.Value);
            }

            var items = await query
                .OrderByDescending(p => p.paymentDate)
                .ThenByDescending(p => p.paymentId)
                .Select(p => new PaymentListItemViewModel
                {
                    PaymentId = p.paymentId,
                    MemberId = p.memberId,
                    MemberName = p.Member != null ? p.Member.fullName : "Unknown Member",
                    MembershipId = p.membershipId,
                    PlanName = p.Membership != null && p.Membership.Plan != null
                        ? p.Membership.Plan.planName
                        : "Unknown Plan",
                    Amount = p.amount,
                    PaymentType = p.paymentType,
                    ReferenceNumber = p.referenceNumber,
                    PaymentDate = p.paymentDate,
                    RecordedByStaffName = p.RecordedByStaff != null ? p.RecordedByStaff.fullName : "Unknown Staff",
                    IsVoided = p.isVoided
                })
                .ToListAsync();

            var totalValid = items
                .Where(i => !i.IsVoided)
                .Sum(i => i.Amount);

            var paymentTypes = new[] { "Cash", "GCash", "Maya", "Bank Transfer", "Card" };
            var plans = await _context.MembershipPlans
                .AsNoTracking()
                .OrderBy(p => p.planName)
                .Select(p => new { p.planId, p.planName })
                .ToListAsync();

            return new PaymentIndexViewModel
            {
                Payments = items,
                MemberSearch = memberSearch,
                FilterDate = filterDate,
                PaymentType = paymentType,
                PlanId = planId,
                TotalValidAmount = totalValid,
                PaymentTypeOptions = paymentTypes.Select(t => new SelectListItem
                {
                    Value = t,
                    Text = t,
                    Selected = string.Equals(t, paymentType, StringComparison.OrdinalIgnoreCase)
                }),
                PlanOptions = plans.Select(p => new SelectListItem
                {
                    Value = p.planId.ToString(),
                    Text = p.planName,
                    Selected = planId == p.planId
                })
            };
        }

        public async Task<PaymentDetailsViewModel?> GetByIdAsync(int id)
        {
            return await _context.Payments
                .AsNoTracking()
                .Include(p => p.Member)
                .Include(p => p.Membership!)
                    .ThenInclude(m => m.Plan)
                .Include(p => p.RecordedByStaff)
                .Where(p => p.paymentId == id)
                .Select(p => new PaymentDetailsViewModel
                {
                    PaymentId = p.paymentId,
                    MemberId = p.memberId,
                    MemberName = p.Member != null ? p.Member.fullName : "Unknown Member",
                    MemberEmail = p.Member != null ? p.Member.userEmail : string.Empty,
                    MembershipId = p.membershipId,
                    PlanName = p.Membership != null && p.Membership.Plan != null
                        ? p.Membership.Plan.planName
                        : "Unknown Plan",
                    MembershipStartDate = p.Membership != null ? p.Membership.startDate : DateTime.MinValue,
                    MembershipEndDate = p.Membership != null ? p.Membership.endDate : DateTime.MinValue,
                    Amount = p.amount,
                    PaymentType = p.paymentType,
                    ReferenceNumber = p.referenceNumber,
                    PaymentDate = p.paymentDate,
                    RecordedByStaffName = p.RecordedByStaff != null ? p.RecordedByStaff.fullName : "Unknown Staff",
                    IsVoided = p.isVoided
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PaymentCreateViewModel> BuildCreateViewModelAsync(
            int? preselectedMemberId = null,
            string? currentUserEmail = null)
        {
            var members = await _context.Members
                .AsNoTracking()
                .Where(m => m.isActive)
                .OrderBy(m => m.fullName)
                .Select(m => new { m.memberId, Display = $"{m.fullName} ({m.userEmail})" })
                .ToListAsync();

            var planRows = await _context.MembershipPlans
                .AsNoTracking()
                .Where(p => p.isActive)
                .OrderBy(p => p.planPrice)
                .Select(p => new { p.planId, p.planName, p.durationDays, p.planPrice })
                .ToListAsync();

            var plans = planRows
                .Select(p => new { p.planId, Display = $"{p.planName} · ₱{p.planPrice:N0}" })
                .ToList();

            var staffMembers = await _context.Staff
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.User != null && s.User.isActive)
                .OrderBy(s => s.fullName)
                .Select(s => new
                {
                    s.StaffID,
                    Email = s.User != null ? s.User.userEmail : string.Empty,
                    Display = $"{s.fullName} ({s.position})"
                })
                .ToListAsync();

            int defaultStaffId = 0;
            if (!string.IsNullOrWhiteSpace(currentUserEmail))
            {
                var matched = staffMembers.FirstOrDefault(s =>
                    string.Equals(s.Email, currentUserEmail, StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    defaultStaffId = matched.StaffID;
                }
            }
            if (defaultStaffId == 0 && staffMembers.Any())
            {
                defaultStaffId = staffMembers.First().StaffID;
            }

            return new PaymentCreateViewModel
            {
                MemberId = preselectedMemberId ?? 0,
                StartDate = DateTime.Today,
                PaymentDate = DateTime.Today,
                RecordedByStaffId = defaultStaffId,
                Members = new SelectList(members, "memberId", "Display", preselectedMemberId),
                Plans = new SelectList(plans, "planId", "Display"),
                StaffList = new SelectList(staffMembers, "StaffID", "Display", defaultStaffId),
                PlanChoices = planRows.Select(p => new PaymentPlanChoiceViewModel
                {
                    PlanId = p.planId,
                    PlanName = p.planName,
                    DurationDays = p.durationDays,
                    PlanPrice = p.planPrice
                }).ToList()
            };
        }

        public async Task<PaymentEditViewModel?> GetForEditAsync(int id)
        {
            return await _context.Payments
                .AsNoTracking()
                .Include(p => p.Member)
                .Include(p => p.Membership!)
                    .ThenInclude(m => m.Plan)
                .Where(p => p.paymentId == id && !p.isVoided)
                .Select(p => new PaymentEditViewModel
                {
                    PaymentId = p.paymentId,
                    MemberName = p.Member != null ? p.Member.fullName : "Unknown Member",
                    PlanName = p.Membership != null && p.Membership.Plan != null
                        ? p.Membership.Plan.planName
                        : "Unknown Plan",
                    Amount = p.amount,
                    PaymentType = p.paymentType,
                    ReferenceNumber = p.referenceNumber,
                    PaymentDate = p.paymentDate
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateAsync(PaymentCreateViewModel model)
        {
            if (model.Amount <= 0m)
            {
                return (false, "Payment amount must be greater than zero.");
            }

            if (!string.Equals(model.PaymentType, "Cash", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(model.ReferenceNumber))
            {
                return (false, "Reference number is required for non-cash payments.");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.memberId == model.MemberId && m.isActive);
            if (member == null)
            {
                return (false, "Selected member does not exist or is inactive.");
            }

            var plan = await _context.MembershipPlans.FirstOrDefaultAsync(p => p.planId == model.PlanId && p.isActive);
            if (plan == null)
            {
                return (false, "Selected membership plan does not exist or is inactive.");
            }

            var staffExists = await _context.Staff.AnyAsync(s => s.StaffID == model.RecordedByStaffId);
            if (!staffExists)
            {
                return (false, "Selected recording staff member does not exist.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var membership = new Memberships
                {
                    memberId = model.MemberId,
                    planId = model.PlanId,
                    startDate = model.StartDate.Date,
                    endDate = model.StartDate.Date.AddDays(plan.durationDays),
                    isActive = true
                };

                _context.Memberships.Add(membership);
                await _context.SaveChangesAsync();

                var payment = new Payments
                {
                    memberId = model.MemberId,
                    membershipId = membership.membershipId,
                    amount = model.Amount,
                    paymentType = model.PaymentType.Trim(),
                    referenceNumber = string.Equals(model.PaymentType, "Cash", StringComparison.OrdinalIgnoreCase)
                        ? null
                        : model.ReferenceNumber?.Trim(),
                    paymentDate = model.PaymentDate.Date,
                    recordedByStaffId = model.RecordedByStaffId,
                    isVoided = false
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return (true, null);
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, "Transaction failed while saving membership and payment. No changes were saved.");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(PaymentEditViewModel model)
        {
            if (model.Amount <= 0m)
            {
                return (false, "Payment amount must be greater than zero.");
            }

            if (!string.Equals(model.PaymentType, "Cash", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(model.ReferenceNumber))
            {
                return (false, "Reference number is required for non-cash payments.");
            }

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.paymentId == model.PaymentId);
            if (payment == null)
            {
                return (false, "Payment record not found.");
            }

            if (payment.isVoided)
            {
                return (false, "Voided payments cannot be edited.");
            }

            payment.amount = model.Amount;
            payment.paymentType = model.PaymentType.Trim();
            payment.referenceNumber = string.Equals(model.PaymentType, "Cash", StringComparison.OrdinalIgnoreCase)
                ? null
                : model.ReferenceNumber?.Trim();
            payment.paymentDate = model.PaymentDate.Date;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> VoidAsync(int id)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.paymentId == id);
            if (payment == null)
            {
                return (false, "Payment record not found.");
            }

            if (payment.isVoided)
            {
                return (false, "This payment has already been voided.");
            }

            payment.isVoided = true;
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}