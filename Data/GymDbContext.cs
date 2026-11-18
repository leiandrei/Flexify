using GymCrudApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Data;

public class GymDbContext : IdentityDbContext
{
    public GymDbContext(DbContextOptions<GymDbContext> options) : base(options) { }

    public new DbSet<Users> Users { get; set; } = null!;
    public DbSet<Members> Members { get; set; } = null!;
    public DbSet<Staff> Staff { get; set; } = null!;
    public DbSet<MembershipPlans> MembershipPlans { get; set; } = null!;
    public DbSet<Memberships> Memberships { get; set; } = null!;
    public DbSet<Payments> Payments { get; set; } = null!;
    public DbSet<Attendance> Attendance { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Two accounts cannot share an email, or login becomes ambiguous.
        modelBuilder.Entity<Users>().HasIndex(u => u.userEmail).IsUnique();

        // Use HasOne(m => m.User) if Members/Staff have the User navigation property;
        // otherwise keep HasOne<Users>()
        modelBuilder.Entity<Members>()
            .HasOne(m => m.User).WithOne().HasForeignKey<Members>(m => m.userId);
        modelBuilder.Entity<Staff>()
            .HasOne(s => s.User).WithOne().HasForeignKey<Staff>(s => s.userId);

        // No cascading deletes: history is kept, rows are deactivated or voided.
        // This also avoids SQL Server's "multiple cascade paths" error on Payments and Memberships.
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Every decimal (plan price, payment amount) is stored as decimal(10,2).
        configurationBuilder.Properties<decimal>().HavePrecision(10, 2);
    }
}