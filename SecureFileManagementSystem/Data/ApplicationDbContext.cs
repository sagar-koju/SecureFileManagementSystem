using Microsoft.EntityFrameworkCore;
using SecureFileManagementSystem.Models;

namespace SecureFileManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Users table for registration/login
        public DbSet<UserRecord> UserRecords { get; set; }

        // File metadata table
        public DbSet<FileMetaData> FileMetaDatas { get; set; }
    }
}
