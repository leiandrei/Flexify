using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GymCrudApp.Models;

namespace GymCrudApp.Data
{
    public class DbInitializer
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly GymDbContext _context;

        public DbInitializer(
            RoleManager<IdentityRole> roleManager,
            UserManager<IdentityUser> userManager,
            GymDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        public async Task SeedAsync()
        {
            // Ensure database and pending migrations are applied
            await _context.Database.MigrateAsync();

            // 1. Seed the three roles (Admin, Staff, Member)
            string[] roles = { "Admin", "Staff", "Member" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed ASP.NET Identity accounts
            var adminIdentity = await EnsureIdentityUserAsync("admin@gmail.com", "Admin@12345", "Admin");
            var staffIdentity = await EnsureIdentityUserAsync("staff@gmail.com", "Staff@12345", "Staff");
            var memberIdentity = await EnsureIdentityUserAsync("juan.delacruz@gmail.com", "Member@12345", "Member");

            // 3. Seed custom Users rows
            if (!await _context.Users.AnyAsync())
            {
                _context.Users.AddRange(
                    new Users
                    {
                        userEmail = "admin@gmail.com",
                        passwordHash = adminIdentity.PasswordHash ?? "SEEDED_IDENTITY",
                        role = "Admin",
                        isActive = true
                    },
                    new Users
                    {
                        userEmail = "staff@gmail.com",
                        passwordHash = staffIdentity.PasswordHash ?? "SEEDED_IDENTITY",
                        role = "Staff",
                        isActive = true
                    },
                    new Users
                    {
                        userEmail = "juan.delacruz@gmail.com",
                        passwordHash = memberIdentity.PasswordHash ?? "SEEDED_IDENTITY",
                        role = "Member",
                        isActive = true
                    }
                );
                await _context.SaveChangesAsync();
            }

            // 4. Seed demo Staff profile
            if (!await _context.Staff.AnyAsync())
            {
                var staffUser = await _context.Users.FirstAsync(u => u.userEmail == "staff@gmail.com");
                _context.Staff.Add(new Staff
                {
                    userId = staffUser.userId,
                    fullName = "Maria Santos",
                    position = "Front Desk & Trainer",
                    dateOfBirth = new DateTime(1996, 5, 14)
                });
                await _context.SaveChangesAsync();
            }

            // seed demo Member profile
            if (!await _context.Members.AnyAsync())
            {
                var memberUser = await _context.Users.FirstAsync(u => u.userEmail == "juan.delacruz@gmail.com");
                _context.Members.Add(new Members
                {
                    userId = memberUser.userId,
                    fullName = "Juan Dela Cruz",
                    contactNumber = "09171234567",
                    birthdate = new DateTime(2000, 8, 22),
                    userEmail = "juan.delacruz@gmail.com",
                    joinDate = DateTime.Today.AddDays(-15),
                    isActive = true
                });
                await _context.SaveChangesAsync();
            }

            // seed demo Membership Plans
            if (!await _context.MembershipPlans.AnyAsync())
            {
                _context.MembershipPlans.AddRange(
                    new MembershipPlans
                    {
                        planName = "Monthly Basic",
                        planPrice = 1200.00m,
                        planDescription = "30-day unlimited gym floor and locker access.",
                        isActive = true
                    },
                    new MembershipPlans
                    {
                        planName = "Quarterly Pro",
                        planPrice = 3200.00m,
                        planDescription = "90-day unlimited access plus 2 personal training sessions.",
                        isActive = true
                    },
                    new MembershipPlans
                    {
                        planName = "Annual VIP",
                        planPrice = 11000.00m,
                        planDescription = "365-day all-access pass with monthly body composition tracking.",
                        isActive = true
                    }
                );
                await _context.SaveChangesAsync();
            }

            // seed an active Membership for demo end-to-end testing
            if (!await _context.Memberships.AnyAsync())
            {
                var demoMember = await _context.Members.FirstAsync();
                var monthlyPlan = await _context.MembershipPlans.FirstAsync(p => p.planName == "Monthly Basic");

                _context.Memberships.Add(new Memberships
                {
                    memberId = demoMember.memberId,
                    planId = monthlyPlan.planId,
                    startDate = DateTime.Today.AddDays(-15),
                    endDate = DateTime.Today.AddDays(15),
                    isActive = true
                });
                await _context.SaveChangesAsync();
            }
        }

        private async Task<IdentityUser> EnsureIdentityUserAsync(string email, string password, string role)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to seed user {email}: {errors}");
                }
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            return user;
        }
    }
}