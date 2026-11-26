using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymCrudApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Data
{
    public class DbInitializer
    {
        private readonly GymDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbInitializer(
            GymDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            await _context.Database.MigrateAsync();

            // 1. Seed Roles
            string[] roles = { "Admin", "Staff", "Member" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Admin & Staff Accounts
            await EnsureUserAccountAsync("admin@gmail.com", "Admin@12345", "Admin");
            var staffUser1 = await EnsureUserAccountAsync("staff@gmail.com", "Staff@12345", "Staff");
            var staffUser2 = await EnsureUserAccountAsync("coach.miguel@gmail.com", "Staff@12345", "Staff");
            var staffUser3 = await EnsureUserAccountAsync("coach.patricia@gmail.com", "Staff@12345", "Staff");

            await EnsureStaffAsync(staffUser1.userId, "Ana Reyes", "Front Desk Officer", new DateTime(1998, 5, 14));
            await EnsureStaffAsync(staffUser2.userId, "Miguel Santos", "Head Fitness Coach", new DateTime(1995, 9, 22));
            await EnsureStaffAsync(staffUser3.userId, "Patricia Gomez", "Strength & Conditioning Coach", new DateTime(1997, 2, 18));

            var primaryStaff = await _context.Staff.OrderBy(s => s.StaffID).FirstAsync();
            var secondaryStaff = await _context.Staff.OrderByDescending(s => s.StaffID).FirstAsync();

            // 3. Seed Membership Plans (Active + 1 Inactive Legacy Plan)
            var dayPass = await EnsurePlanAsync(
                "Day Pass", 150.00m, 1,
                "Single-day full gym floor access to weights and cardio equipment.", true);

            var monthlyBasic = await EnsurePlanAsync(
                "Monthly Basic", 1500.00m, 30,
                "30-day unlimited access to free weights, machines, and cardio zone.", true);

            var studentMonthly = await EnsurePlanAsync(
                "Student Saver", 1100.00m, 30,
                "30-day discounted gym floor pass for valid student ID holders.", true);

            var quarterlyPro = await EnsurePlanAsync(
                "Quarterly Pro", 4000.00m, 90,
                "90-day full access plus group HIIT, spin, and mobility classes.", true);

            var annualElite = await EnsurePlanAsync(
                "Annual Elite", 14000.00m, 365,
                "365-day all-access pass with dedicated locker and 2 coaching sessions.", true);

            await EnsurePlanAsync(
                "Founders Founding Promo", 999.00m, 30,
                "Archived grand-opening promotional tier (no longer offered).", false);

            // 4. Seed Demo Members (Active, Expiring, Expired, Unassigned, and Inactive)
            var carlos = await EnsureMemberAsync(
                "carlos.mendoza@gmail.com", "Member@12345",
                "Carlos Mendoza", "09171112233", new DateTime(1999, 3, 12),
                DateTime.Today.AddDays(-65), true);

            var bianca = await EnsureMemberAsync(
                "bianca.lim@gmail.com", "Member@12345",
                "Bianca Lim", "09182223344", new DateTime(2001, 7, 25),
                DateTime.Today.AddDays(-27), true);

            var rafael = await EnsureMemberAsync(
                "rafael.torres@gmail.com", "Member@12345",
                "Rafael Torres", "09193334455", new DateTime(1996, 11, 8),
                DateTime.Today.AddDays(-50), true);

            var sofia = await EnsureMemberAsync(
                "sofia.garcia@gmail.com", "Member@12345",
                "Sofia Garcia", "09204445566", new DateTime(2002, 1, 30),
                DateTime.Today.AddDays(-2), true);

            var marco = await EnsureMemberAsync(
                "marco.delacruz@gmail.com", "Member@12345",
                "Marco Dela Cruz", "09175556677", new DateTime(1994, 6, 19),
                DateTime.Today.AddDays(-120), true);

            var Camille = await EnsureMemberAsync(
                "camille.reyes@gmail.com", "Member@12345",
                "Camille Reyes", "09186667788", new DateTime(2000, 10, 4),
                DateTime.Today.AddDays(-25), true);

            var joshua = await EnsureMemberAsync(
                "joshua.villanueva@gmail.com", "Member@12345",
                "Joshua Villanueva", "09197778899", new DateTime(2003, 4, 15),
                DateTime.Today.AddDays(-18), true);

            var isabel = await EnsureMemberAsync(
                "isabel.aquino@gmail.com", "Member@12345",
                "Isabel Aquino", "09208889900", new DateTime(1997, 12, 11),
                DateTime.Today.AddDays(-42), true);

            var paolo = await EnsureMemberAsync(
                "paolo.bautista@gmail.com", "Member@12345",
                "Paolo Bautista", "09179990011", new DateTime(1998, 8, 27),
                DateTime.Today.AddDays(-10), true);

            await EnsureMemberAsync(
                "denise.navarro@gmail.com", "Member@12345",
                "Denise Navarro", "09181012020", new DateTime(1995, 2, 9),
                DateTime.Today.AddDays(-95), false);

            // 5. Seed Memberships & Payments across all statuses and payment methods
            var carlosSub = await EnsureMembershipAsync(
                carlos.memberId, quarterlyPro.planId,
                DateTime.Today.AddDays(-15), DateTime.Today.AddDays(75), true);
            await EnsurePaymentAsync(
                carlos.memberId, carlosSub.membershipId, quarterlyPro.planPrice,
                "GCash", "GCASH-20261109-8841", carlosSub.startDate, primaryStaff.StaffID, false);

            var biancaSub = await EnsureMembershipAsync(
                bianca.memberId, monthlyBasic.planId,
                DateTime.Today.AddDays(-27), DateTime.Today.AddDays(3), true);
            await EnsurePaymentAsync(
                bianca.memberId, biancaSub.membershipId, monthlyBasic.planPrice,
                "Cash", null, biancaSub.startDate, primaryStaff.StaffID, false);

            var rafaelSub = await EnsureMembershipAsync(
                rafael.memberId, monthlyBasic.planId,
                DateTime.Today.AddDays(-40), DateTime.Today.AddDays(-10), true);
            await EnsurePaymentAsync(
                rafael.memberId, rafaelSub.membershipId, monthlyBasic.planPrice,
                "Maya", "MAYA-20261015-4410", rafaelSub.startDate, primaryStaff.StaffID, false);

            var marcoSub = await EnsureMembershipAsync(
                marco.memberId, annualElite.planId,
                DateTime.Today.AddDays(-90), DateTime.Today.AddDays(275), true);
            await EnsurePaymentAsync(
                marco.memberId, marcoSub.membershipId, annualElite.planPrice,
                "Bank Transfer", "BPI-INSTAPAY-77219", marcoSub.startDate, secondaryStaff.StaffID, false);

            var camilleSub = await EnsureMembershipAsync(
                Camille.memberId, monthlyBasic.planId,
                DateTime.Today.AddDays(-25), DateTime.Today.AddDays(5), true);
            await EnsurePaymentAsync(
                Camille.memberId, camilleSub.membershipId, monthlyBasic.planPrice,
                "GCash", "GCASH-20261030-3109", camilleSub.startDate, primaryStaff.StaffID, false);

            var joshuaSub = await EnsureMembershipAsync(
                joshua.memberId, studentMonthly.planId,
                DateTime.Today.AddDays(-18), DateTime.Today.AddDays(12), true);
            await EnsurePaymentAsync(
                joshua.memberId, joshuaSub.membershipId, studentMonthly.planPrice,
                "Maya", "MAYA-20261106-9052", joshuaSub.startDate, secondaryStaff.StaffID, false);

            var isabelSub = await EnsureMembershipAsync(
                isabel.memberId, quarterlyPro.planId,
                DateTime.Today.AddDays(-35), DateTime.Today.AddDays(55), true);
            // Voided duplicate entry + valid Card payment for realistic payment audit history
            await EnsurePaymentAsync(
                isabel.memberId, isabelSub.membershipId, quarterlyPro.planPrice,
                "Card", "VISA-VOID-00412", isabelSub.startDate, primaryStaff.StaffID, true);
            await EnsurePaymentAsync(
                isabel.memberId, isabelSub.membershipId, quarterlyPro.planPrice,
                "Card", "VISA-AUTH-99814", isabelSub.startDate, primaryStaff.StaffID, false);

            var paoloSub = await EnsureMembershipAsync(
                paolo.memberId, dayPass.planId,
                DateTime.Today, DateTime.Today.AddDays(1), true);
            await EnsurePaymentAsync(
                paolo.memberId, paoloSub.membershipId, dayPass.planPrice,
                "Cash", null, DateTime.Today, primaryStaff.StaffID, false);

            // 6. Seed 7-Day Attendance History + Live Floor Check-Ins
            var attendanceSeeds = new List<(int MemberId, DateTime CheckIn, DateTime? CheckOut)>
            {
                // 6 days ago
                (carlos.memberId, DateTime.Today.AddDays(-6).AddHours(7), DateTime.Today.AddDays(-6).AddHours(8).AddMinutes(40)),
                (marco.memberId, DateTime.Today.AddDays(-6).AddHours(17), DateTime.Today.AddDays(-6).AddHours(19)),
                // 5 days ago
                (bianca.memberId, DateTime.Today.AddDays(-5).AddHours(9), DateTime.Today.AddDays(-5).AddHours(10).AddMinutes(30)),
                (isabel.memberId, DateTime.Today.AddDays(-5).AddHours(16), DateTime.Today.AddDays(-5).AddHours(17).AddMinutes(45)),
                (joshua.memberId, DateTime.Today.AddDays(-5).AddHours(18), DateTime.Today.AddDays(-5).AddHours(19).AddMinutes(30)),
                // 4 days ago
                (carlos.memberId, DateTime.Today.AddDays(-4).AddHours(7).AddMinutes(15), DateTime.Today.AddDays(-4).AddHours(8).AddMinutes(50)),
                (Camille.memberId, DateTime.Today.AddDays(-4).AddHours(10), DateTime.Today.AddDays(-4).AddHours(11).AddMinutes(20)),
                (marco.memberId, DateTime.Today.AddDays(-4).AddHours(17).AddMinutes(30), DateTime.Today.AddDays(-4).AddHours(19).AddMinutes(15)),
                (isabel.memberId, DateTime.Today.AddDays(-4).AddHours(18), DateTime.Today.AddDays(-4).AddHours(19).AddMinutes(40)),
                // 3 days ago
                (joshua.memberId, DateTime.Today.AddDays(-3).AddHours(8), DateTime.Today.AddDays(-3).AddHours(9).AddMinutes(30)),
                (bianca.memberId, DateTime.Today.AddDays(-3).AddHours(15), DateTime.Today.AddDays(-3).AddHours(16).AddMinutes(30)),
                (carlos.memberId, DateTime.Today.AddDays(-3).AddHours(17), DateTime.Today.AddDays(-3).AddHours(18).AddMinutes(45)),
                // 2 days ago
                (marco.memberId, DateTime.Today.AddDays(-2).AddHours(6).AddMinutes(45), DateTime.Today.AddDays(-2).AddHours(8).AddMinutes(15)),
                (Camille.memberId, DateTime.Today.AddDays(-2).AddHours(11), DateTime.Today.AddDays(-2).AddHours(12).AddMinutes(25)),
                (isabel.memberId, DateTime.Today.AddDays(-2).AddHours(16).AddMinutes(30), DateTime.Today.AddDays(-2).AddHours(18)),
                (joshua.memberId, DateTime.Today.AddDays(-2).AddHours(18).AddMinutes(15), DateTime.Today.AddDays(-2).AddHours(19).AddMinutes(50)),
                (bianca.memberId, DateTime.Today.AddDays(-2).AddHours(19), DateTime.Today.AddDays(-2).AddHours(20).AddMinutes(15)),
                // Yesterday
                (carlos.memberId, DateTime.Today.AddDays(-1).AddHours(7).AddMinutes(30), DateTime.Today.AddDays(-1).AddHours(9)),
                (marco.memberId, DateTime.Today.AddDays(-1).AddHours(12), DateTime.Today.AddDays(-1).AddHours(13).AddMinutes(30)),
                (isabel.memberId, DateTime.Today.AddDays(-1).AddHours(17), DateTime.Today.AddDays(-1).AddHours(18).AddMinutes(45)),
                (Camille.memberId, DateTime.Today.AddDays(-1).AddHours(18), DateTime.Today.AddDays(-1).AddHours(19).AddMinutes(20)),
                // Today (enough visits to exercise paging + 2 active check-ins on the gym floor)
                (carlos.memberId, DateTime.Today.AddHours(6).AddMinutes(5), DateTime.Today.AddHours(7).AddMinutes(10)),
                (bianca.memberId, DateTime.Today.AddHours(6).AddMinutes(31), DateTime.Today.AddHours(7).AddMinutes(20)),
                (isabel.memberId, DateTime.Today.AddHours(6).AddMinutes(40), DateTime.Today.AddHours(7).AddMinutes(55)),
                (Camille.memberId, DateTime.Today.AddHours(7).AddMinutes(2), DateTime.Today.AddHours(8).AddMinutes(15)),
                (carlos.memberId, DateTime.Today.AddHours(7).AddMinutes(30), DateTime.Today.AddHours(9)),
                (marco.memberId, DateTime.Today.AddHours(10).AddMinutes(15), DateTime.Today.AddHours(11).AddMinutes(45)),
                (joshua.memberId, DateTime.Now.AddMinutes(-55), null),
                (paolo.memberId, DateTime.Now.AddMinutes(-20), null)
            };

            foreach (var (memberId, checkIn, checkOut) in attendanceSeeds)
            {
                var exists = await _context.Attendance.AnyAsync(a =>
                    a.memberId == memberId && a.checkIn == checkIn);

                if (!exists)
                {
                    _context.Attendance.Add(new Attendance
                    {
                        memberId = memberId,
                        checkIn = checkIn,
                        checkOut = checkOut
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task<Users> EnsureUserAccountAsync(string email, string password, string role, bool isActive = true)
        {
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser == null)
            {
                identityUser = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    LockoutEnd = isActive ? null : DateTimeOffset.MaxValue
                };
                await _userManager.CreateAsync(identityUser, password);
            }

            if (!await _userManager.IsInRoleAsync(identityUser, role))
            {
                await _userManager.AddToRoleAsync(identityUser, role);
            }

            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.userEmail == email);
            if (appUser == null)
            {
                appUser = new Users
                {
                    userEmail = email,
                    passwordHash = identityUser.PasswordHash ?? "MANAGED_BY_IDENTITY",
                    role = role,
                    isActive = isActive
                };
                _context.Users.Add(appUser);
                await _context.SaveChangesAsync();
            }

            return appUser;
        }

        private async Task EnsureStaffAsync(int userId, string fullName, string position, DateTime dob)
        {
            if (await _context.Staff.AnyAsync(s => s.userId == userId))
            {
                return;
            }

            _context.Staff.Add(new Staff
            {
                userId = userId,
                fullName = fullName,
                position = position,
                dateOfBirth = dob
            });
            await _context.SaveChangesAsync();
        }

        private async Task<MembershipPlans> EnsurePlanAsync(
            string name, decimal price, int durationDays, string description, bool isActive)
        {
            var existing = await _context.MembershipPlans.FirstOrDefaultAsync(p => p.planName == name);
            if (existing != null)
            {
                return existing;
            }

            var plan = new MembershipPlans
            {
                planName = name,
                planPrice = price,
                durationDays = durationDays,
                planDescription = description,
                isActive = isActive
            };
            _context.MembershipPlans.Add(plan);
            await _context.SaveChangesAsync();
            return plan;
        }

        private async Task<Members> EnsureMemberAsync(
            string email, string password, string fullName, string contactNumber,
            DateTime birthdate, DateTime joinDate, bool isActive)
        {
            var existing = await _context.Members.FirstOrDefaultAsync(m => m.userEmail == email);
            if (existing != null)
            {
                return existing;
            }

            var user = await EnsureUserAccountAsync(email, password, "Member", isActive);
            var member = new Members
            {
                userId = user.userId,
                fullName = fullName,
                userEmail = email,
                contactNumber = contactNumber,
                birthdate = birthdate,
                joinDate = joinDate,
                isActive = isActive
            };
            _context.Members.Add(member);
            await _context.SaveChangesAsync();
            return member;
        }

        private async Task<Memberships> EnsureMembershipAsync(
            int memberId, int planId, DateTime startDate, DateTime endDate, bool isActive)
        {
            var existing = await _context.Memberships
                .OrderByDescending(m => m.endDate)
                .FirstOrDefaultAsync(m => m.memberId == memberId);

            if (existing != null)
            {
                return existing;
            }

            var sub = new Memberships
            {
                memberId = memberId,
                planId = planId,
                startDate = startDate,
                endDate = endDate,
                isActive = isActive
            };
            _context.Memberships.Add(sub);
            await _context.SaveChangesAsync();
            return sub;
        }

        private async Task EnsurePaymentAsync(
            int memberId, int membershipId, decimal amount, string paymentType,
            string? referenceNumber, DateTime paymentDate, int staffId, bool isVoided)
        {
            var exists = referenceNumber != null
                ? await _context.Payments.AnyAsync(p => p.referenceNumber == referenceNumber)
                : await _context.Payments.AnyAsync(p =>
                    p.memberId == memberId &&
                    p.membershipId == membershipId &&
                    p.paymentType == paymentType &&
                    p.isVoided == isVoided);

            if (exists)
            {
                return;
            }

            _context.Payments.Add(new Payments
            {
                memberId = memberId,
                membershipId = membershipId,
                amount = amount,
                paymentType = paymentType,
                referenceNumber = referenceNumber,
                paymentDate = paymentDate,
                recordedByStaffId = staffId,
                isVoided = isVoided
            });
            await _context.SaveChangesAsync();
        }
    }
}