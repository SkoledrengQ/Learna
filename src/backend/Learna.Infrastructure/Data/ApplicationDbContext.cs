using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Learna.Core.Entities;

namespace Learna.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students { get; set; }
    public DbSet<Guardian> Guardians { get; set; }
    public DbSet<StudentGuardian> StudentGuardians { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<SchoolYear> SchoolYears { get; set; }
    public DbSet<Term> Terms { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    private static void ConfigurePersonName<TEntity>(OwnedNavigationBuilder<TEntity, PersonName> name)
        where TEntity : class
    {
        name.Property(n => n.Title).HasMaxLength(50);
        name.Property(n => n.FirstName).IsRequired().HasMaxLength(100);
        name.Property(n => n.LastName).IsRequired().HasMaxLength(100);
        name.Property(n => n.FirstNameEnglish).HasMaxLength(100);
        name.Property(n => n.LastNameEnglish).HasMaxLength(100);
        name.Property(n => n.Nickname).HasMaxLength(100);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StudentId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IdCardNumber).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.StudentId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IdCardNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Height).HasPrecision(5, 2);
            entity.Property(e => e.Weight).HasPrecision(5, 2);

            entity.OwnsOne(e => e.Name, name => ConfigurePersonName(name));
        });

        // Guardian configuration
        modelBuilder.Entity<Guardian>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);

            entity.OwnsOne(e => e.Name, name => ConfigurePersonName(name));
        });

        // StudentGuardian join configuration (many-to-many with relationship metadata)
        modelBuilder.Entity<StudentGuardian>(entity =>
        {
            entity.HasKey(sg => new { sg.StudentId, sg.GuardianId });
            entity.Property(e => e.Relationship).IsRequired().HasMaxLength(50);

            entity.HasOne(sg => sg.Student)
                .WithMany(s => s.StudentGuardians)
                .HasForeignKey(sg => sg.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sg => sg.Guardian)
                .WithMany(g => g.StudentGuardians)
                .HasForeignKey(sg => sg.GuardianId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Teacher configuration
        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.EmployeeId).HasMaxLength(50);

            entity.OwnsOne(e => e.Name, name => ConfigurePersonName(name));
        });

        // SchoolYear configuration
        modelBuilder.Entity<SchoolYear>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsArchived).HasDefaultValue(false);
        });

        // Term configuration
        modelBuilder.Entity<Term>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);

            entity.HasOne(t => t.SchoolYear)
                .WithMany(sy => sy.Terms)
                .HasForeignKey(t => t.SchoolYearId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Subject configuration
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.NameEnglish).IsRequired().HasMaxLength(200);
            entity.Property(e => e.NameThai).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            // One-to-one relationship with Student (optional)
            entity.HasOne(e => e.Student)
                .WithOne(s => s.User)
                .HasForeignKey<User>(u => u.StudentId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-one relationship with Guardian (optional; guardian login is a later work order)
            entity.HasOne(e => e.Guardian)
                .WithOne(g => g.User)
                .HasForeignKey<User>(u => u.GuardianId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-one relationship with Teacher (optional; teacher login is a later work order)
            entity.HasOne(e => e.Teacher)
                .WithOne(t => t.User)
                .HasForeignKey<User>(u => u.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(255);

            // Seed default roles
            entity.HasData(
                new Role { Id = 1, Name = "Admin", Description = "System administrator" },
                new Role { Id = 2, Name = "Teacher", Description = "School teacher" },
                new Role { Id = 3, Name = "Student", Description = "School student" },
                new Role { Id = 4, Name = "Parent", Description = "Student parent" }
            );
        });

        // UserRole configuration (many-to-many)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).IsRequired().HasMaxLength(255);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
