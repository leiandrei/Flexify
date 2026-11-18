using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Data;

public class GymDBContext : IdentityDbContext
{
    public GymDBContext(DbContextOptions<GymDBContext> options) : base(options) { }
}