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
    public DbSet<SchoolClass> SchoolClasses { get; set; }
    public DbSet<ClassMembership> ClassMemberships { get; set; }
    public DbSet<SubjectGroup> SubjectGroups { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<LessonRule> LessonRules { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<FileResource> FileResources { get; set; }
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

        // SchoolClass configuration (administrative/homeroom group)
        modelBuilder.Entity<SchoolClass>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.SchoolYear)
                .WithMany()
                .HasForeignKey(e => e.SchoolYearId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.HomeroomTeacher)
                .WithMany()
                .HasForeignKey(e => e.HomeroomTeacherId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ClassMembership configuration (Student <-> SchoolClass, history-preserving)
        modelBuilder.Entity<ClassMembership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentId, e.LeftDate });
            entity.HasIndex(e => e.ClassId);

            entity.HasOne(e => e.Student)
                .WithMany(s => s.ClassMemberships)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Class)
                .WithMany(c => c.Memberships)
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SubjectGroup configuration (teaching group)
        modelBuilder.Entity<SubjectGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);

            entity.HasOne(e => e.Subject)
                .WithMany()
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Term)
                .WithMany()
                .HasForeignKey(e => e.TermId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Teacher)
                .WithMany()
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Enrollment configuration (Student <-> SubjectGroup, history-preserving)
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentId, e.UnenrolledDate });
            entity.HasIndex(e => e.SubjectGroupId);

            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SubjectGroup)
                .WithMany(sg => sg.Enrollments)
                .HasForeignKey(e => e.SubjectGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Room configuration
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Building).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });

        // LessonRule configuration (recurring weekly rule that generates Lessons)
        modelBuilder.Entity<LessonRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SubjectGroupId);

            entity.HasOne(e => e.SubjectGroup)
                .WithMany()
                .HasForeignKey(e => e.SubjectGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Room)
                .WithMany()
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Lesson configuration (materialized class session, rule-sourced or one-off)
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.SubjectGroupId);
            entity.HasIndex(e => e.RoomId);
            entity.HasIndex(e => e.TeacherId);
            entity.HasIndex(e => e.SourceRuleId);

            entity.HasOne(e => e.SubjectGroup)
                .WithMany()
                .HasForeignKey(e => e.SubjectGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Room)
                .WithMany()
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Teacher)
                .WithMany()
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.SourceRule)
                .WithMany(r => r.Lessons)
                .HasForeignKey(e => e.SourceRuleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.LessonId, e.StudentId }).IsUnique();
            entity.HasIndex(e => e.StudentId);
            entity.Property(e => e.Note).HasMaxLength(1000);

            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.AttendanceRecords)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Student)
                .WithMany(s => s.AttendanceRecords)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.RecordedByUser)
                .WithMany()
                .HasForeignKey(e => e.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FileResource>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.StoredPath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.StoredName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasIndex(e => e.SubjectGroupId);
            entity.HasIndex(e => e.LessonId);
            entity.HasIndex(e => e.UploadedByUserId);
            entity.ToTable(t => t.HasCheckConstraint("CK_FileResource_ExactlyOneTarget", "(`SubjectGroupId` IS NULL) <> (`LessonId` IS NULL)"));
            entity.HasOne(e => e.UploadedByUser).WithMany().HasForeignKey(e => e.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SubjectGroup).WithMany().HasForeignKey(e => e.SubjectGroupId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Lesson).WithMany().HasForeignKey(e => e.LessonId).OnDelete(DeleteBehavior.Cascade);
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PreferredLanguage).HasMaxLength(10);

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
