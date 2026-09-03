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
        public DbSet<ActivityLog> ActivityLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Şu an "bu username var mı" kontrolü sadece validator'da (C# kodunda) yapılıyor: önce veritabanına sorulur, "yok" cevabı gelirse kayıt eklenir. Ama bu iki adım arasında çok küçük bir an var — tam o anda başka bir istek de aynı soruyu sorup "yok" cevabını alırsa, ikisi de "ekleyebilirim" sanıp ikisi de ekler. Unique constraint, bu kontrolü veritabanının kendisine yaptırmak demek: "bu alana asla aynı değerle iki kayıt giremezsin" diye DB seviyesinde bir kural koyarız, uygulama kodu ne yaparsa yapsın veritabanı ikinci kaydı reddeder — validator'ın üstüne ekstra bir güvenlik katmanı.
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.RegistrationNumber)
                .IsUnique();
        }






    }
}