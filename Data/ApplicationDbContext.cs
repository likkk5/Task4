using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using UserManagement.Models;

namespace UserManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email_Unique");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.LastLoginTime)
                .HasDatabaseName("IX_Users_LastLoginTime");

            modelBuilder.Entity<User>()
                .Property(u => u.Status)
                .HasDefaultValue("unverified");

            modelBuilder.Entity<User>()
                .Property(u => u.RegistrationTime)
                .HasDefaultValueSql("NOW()");
        }
    }
}