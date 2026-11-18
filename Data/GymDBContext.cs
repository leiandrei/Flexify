using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Data;

public class GymDBContext : IdentityDbContext<IdentityUser>
{
    public GymDBContext(DbContextOptions<GymDBContext> options) : base(options) { }
}