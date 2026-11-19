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

            // 2. Seed Admin & Staff Accounts (Identity + Users + Staff)
            await EnsureUserAccountAsync("admin@gmail.com", "Admin@12345", "Admin");
            var staffUser1 = await EnsureUserAccountAsync("staff@gmail.com", "Staff@12345", "Staff");
            var staffUser2 = await EnsureUserAccountAsync("coach.miguel@gmail.com", "Staff@12345", "Staff");

            if (!await _context.Staff.AnyAsync())
            {
                _context.Staff.AddRange(
                    new Staff
                    {
                        userId = staffUser1.userId,
                        fullName = "Ana Reyes",
                        position = "Front Desk Officer",
                        dateOfBirth = new DateTime(1998, 5, 14)
                    },
                    new Staff
                    {
                        userId = staffUser2.userId,
                        fullName = "Miguel Santos",
                        position = "Head Fitness Coach",
                        dateOfBirth = new DateTime(1995, 9, 22)
                    }
                );
                await _context.SaveChangesAsync();
            }

            // 3. Seed Membership Plans (distinct durations for EndDate calculation)
            if (!await _context.MembershipPlans.AnyAsync())
            {
                _context.MembershipPlans.AddRange(
                    new MembershipPlans
                    {
                        planName = "Day Pass",
                        planPrice = 150.00m,
                        durationDays = 1,
                        planDescription = "Single-day full gym floor access.",
                        isActive = true
                    },
                    new MembershipPlans
                    {
                        planName = "Monthly Basic",
                        planPrice = 1500.00m,
                        durationDays = 30,
                        planDescription = "30-day access to weights and cardio zone.",
                        isActive = true
                    },
                    new MembershipPlans
                    {
                        planName = "Quarterly Pro",
                        planPrice = 4000.00m,
                        durationDays = 90,
                        planDescription = "90-day full access plus group fitness classes.",
                        isActive = true
                    },
                    new MembershipPlans
                    {
                        planName = "Annual Elite",
                        planPrice = 14000.00m,
                        durationDays = 365,
                        planDescription = "365-day all-access pass with locker privileges.",
                        isActive = true
                    }
                );
                await _context.SaveChangesAsync();
            }

            // 4. Seed Demo Members covering Active, Expiring, Expired, and Unassigned cases
            if (!await _context.Members.AnyAsync())
            {
                var m1User = await EnsureUserAccountAsync("carlos.mendoza@gmail.com", "Member@12345", "Member");
                var m2User = await EnsureUserAccountAsync("bianca.lim@gmail.com", "Member@12345", "Member");
                var m3User = await EnsureUserAccountAsync("rafael.torres@gmail.com", "Member@12345", "Member");
                var m4User = await EnsureUserAccountAsync("sofia.garcia@gmail.com", "Member@12345", "Member");

                _context.Members.AddRange(
                    new Members
                    {
                        userId = m1User.userId,
                        fullName = "Carlos Mendoza",
                        userEmail = "carlos.mendoza@gmail.com",
                        contactNumber = "09171112233",
                        birthdate = new DateTime(1999, 3, 12),
                        joinDate = DateTime.Today.AddDays(-20),
                        isActive = true
                    },
                    new Members
                    {
                        userId = m2User.userId,
                        fullName = "Bianca Lim",
                        userEmail = "bianca.lim@gmail.com",
                        contactNumber = "09182223344",
                        birthdate = new DateTime(2001, 7, 25),
                        joinDate = DateTime.Today.AddDays(-27),
                        isActive = true
                    },
                    new Members
                    {
                        userId = m3User.userId,
                        fullName = "Rafael Torres",
                        userEmail = "rafael.torres@gmail.com",
                        contactNumber = "09193334455",
                        birthdate = new DateTime(1996, 11, 8),
                        joinDate = DateTime.Today.AddDays(-45),
                        isActive = true
                    },
                    new Members
                    {
                        userId = m4User.userId,
                        fullName = "Sofia Garcia",
                        userEmail = "sofia.garcia@gmail.com",
                        contactNumber = "09204445566",
                        birthdate = new DateTime(2002, 1, 30),
                        joinDate = DateTime.Today,
                        isActive = true
                    }
                );
                await _context.SaveChangesAsync();
            }

            // 5. Seed Memberships & Payments (Active, Expiring in 3 days, Expired 10 days ago)
            if (!await _context.Memberships.AnyAsync())
            {
                var monthlyPlan = await _context.MembershipPlans.FirstAsync(p => p.planName == "Monthly Basic");
                var quarterlyPlan = await _context.MembershipPlans.FirstAsync(p => p.planName == "Quarterly Pro");
                var primaryStaff = await _context.Staff.FirstAsync();

                var carlos = await _context.Members.FirstAsync(m => m.userEmail == "carlos.mendoza@gmail.com");
                var bianca = await _context.Members.FirstAsync(m => m.userEmail == "bianca.lim@gmail.com");
                var rafael = await _context.Members.FirstAsync(m => m.userEmail == "rafael.torres@gmail.com");

                var activeSub = new Memberships
                {
                    memberId = carlos.memberId,
                    planId = quarterlyPlan.planId,
                    startDate = DateTime.Today.AddDays(-15),
                    endDate = DateTime.Today.AddDays(75),
                    isActive = true
                };

                var expiringSub = new Memberships
                {
                    memberId = bianca.memberId,
                    planId = monthlyPlan.planId,
                    startDate = DateTime.Today.AddDays(-27),
                    endDate = DateTime.Today.AddDays(3), // Triggers 7-day Expiring warning
                    isActive = true
                };

                var expiredSub = new Memberships
                {
                    memberId = rafael.memberId,
                    planId = monthlyPlan.planId,
                    startDate = DateTime.Today.AddDays(-40),
                    endDate = DateTime.Today.AddDays(-10), // Triggers Expired check-in block
                    isActive = true
                };

                _context.Memberships.AddRange(activeSub, expiringSub, expiredSub);
                await _context.SaveChangesAsync();

                if (!await _context.Payments.AnyAsync())
                {
                    _context.Payments.AddRange(
                        new Payments
                        {
                            memberId = carlos.memberId,
                            membershipId = activeSub.membershipId,
                            amount = quarterlyPlan.planPrice,
                            paymentType = "GCash",
                            referenceNumber = "GCASH-20261105-001",
                            paymentDate = activeSub.startDate,
                            recordedByStaffId = primaryStaff.StaffID,
                            isVoided = false
                        },
                        new Payments
                        {
                            memberId = bianca.memberId,
                            membershipId = expiringSub.membershipId,
                            amount = monthlyPlan.planPrice,
                            paymentType = "Cash",
                            referenceNumber = null,
                            paymentDate = expiringSub.startDate,
                            recordedByStaffId = primaryStaff.StaffID,
                            isVoided = false
                        },
                        new Payments
                        {
                            memberId = rafael.memberId,
                            membershipId = expiredSub.membershipId,
                            amount = monthlyPlan.planPrice,
                            paymentType = "Maya",
                            referenceNumber = "MAYA-20261011-009",
                            paymentDate = expiredSub.startDate,
                            recordedByStaffId = primaryStaff.StaffID,
                            isVoided = false
                        }
                    );
                    await _context.SaveChangesAsync();
                }
            }

            // 6. Seed Sample Attendance Logs for Today
            if (!await _context.Attendance.AnyAsync())
            {
                var carlos = await _context.Members.FirstAsync(m => m.userEmail == "carlos.mendoza@gmail.com");
                _context.Attendance.Add(new Attendance
                {
                    memberId = carlos.memberId,
                    checkIn = DateTime.Today.AddHours(7).AddMinutes(30),
                    checkOut = DateTime.Today.AddHours(9).AddMinutes(0)
                });
                await _context.SaveChangesAsync();
            }
        }

        private async Task<Users> EnsureUserAccountAsync(string email, string password, string role)
        {
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser == null)
            {
                identityUser = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
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
                    isActive = true
                };
                _context.Users.Add(appUser);
                await _context.SaveChangesAsync();
            }

            return appUser;
        }
    }
}