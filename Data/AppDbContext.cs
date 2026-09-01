using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Models;

namespace EmployeeManagement.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; } // buradaki employee modelimiz ile veritanındaki employees tablosunu bağladık 
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Title> Titles { get; set; }
        
        public DbSet<RequestLog> RequestLogs { get; set; }
    }
}