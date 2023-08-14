using Microsoft.EntityFrameworkCore;
using SelfServicePassword.Models;

namespace SelfServicePassword.DAL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Admin> Admins { get; set; }
    }
}
