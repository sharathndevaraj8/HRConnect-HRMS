using HRConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRConnect.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDepartments(modelBuilder);
        ConfigureEmployees(modelBuilder);
        ConfigureLeave(modelBuilder);
        ConfigureIdentity(modelBuilder);
        SeedReferenceData(modelBuilder);
    }

    private static void ConfigureDepartments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Code).HasMaxLength(20);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.Code).IsUnique();
        });
    }

    private static void ConfigureEmployees(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(x => x.EmployeeCode).HasMaxLength(30);
            entity.Property(x => x.FirstName).HasMaxLength(50);
            entity.Property(x => x.LastName).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(255);
            entity.Property(x => x.PersonalEmail).HasMaxLength(255);
            entity.Property(x => x.PhoneNumber).HasMaxLength(20);
            entity.Property(x => x.AlternatePhoneNumber).HasMaxLength(20);
            entity.Property(x => x.Gender).HasMaxLength(30);
            entity.Property(x => x.MaritalStatus).HasMaxLength(30);
            entity.Property(x => x.BloodGroup).HasMaxLength(10);
            entity.Property(x => x.Designation).HasMaxLength(100);
            entity.Property(x => x.EmploymentType).HasMaxLength(30);
            entity.Property(x => x.EmploymentStatus).HasMaxLength(30);
            entity.Property(x => x.WorkLocation).HasMaxLength(100);
            entity.Property(x => x.AddressLine1).HasMaxLength(255);
            entity.Property(x => x.AddressLine2).HasMaxLength(255);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.State).HasMaxLength(100);
            entity.Property(x => x.PostalCode).HasMaxLength(12);
            entity.Property(x => x.Country).HasMaxLength(100);
            entity.Property(x => x.EmergencyContactName).HasMaxLength(100);
            entity.Property(x => x.EmergencyContactRelationship).HasMaxLength(50);
            entity.Property(x => x.EmergencyContactPhone).HasMaxLength(20);
            entity.HasIndex(x => x.EmployeeCode).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();

            entity.HasOne(x => x.Department)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Manager)
                .WithMany(x => x.DirectReports)
                .HasForeignKey(x => x.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeDocument>(entity =>
        {
            entity.Property(x => x.DocumentType).HasMaxLength(50);
            entity.Property(x => x.OriginalFileName).HasMaxLength(255);
            entity.Property(x => x.StoredFileName).HasMaxLength(255);
            entity.Property(x => x.ContentType).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => new { x.EmployeeId, x.DocumentType });
            entity.HasOne(x => x.Employee)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.UploadedByUserAccount)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureLeave(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(30);
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.ApplicableGender).HasMaxLength(30);
            entity.Property(x => x.AnnualEntitlement).HasPrecision(6, 2);
            entity.Property(x => x.CarryForwardLimit).HasPrecision(6, 2);
            entity.Property(x => x.MaxConsecutiveDays).HasPrecision(6, 2);
            entity.Property(x => x.DocumentRequiredAfterDays).HasPrecision(6, 2);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.Property(x => x.OpeningBalance).HasPrecision(7, 2);
            entity.Property(x => x.Accrued).HasPrecision(7, 2);
            entity.Property(x => x.Used).HasPrecision(7, 2);
            entity.Property(x => x.Adjustment).HasPrecision(7, 2);
            entity.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.Year }).IsUnique();
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.LeaveType).WithMany(x => x.LeaveBalances).HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.Property(x => x.NumberOfDays).HasPrecision(6, 2);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.ContactDuringLeave).HasMaxLength(100);
            entity.Property(x => x.ReviewComment).HasMaxLength(1000);
            entity.HasIndex(x => new { x.EmployeeId, x.StartDate, x.EndDate });
            entity.HasOne(x => x.Employee).WithMany(x => x.LeaveRequests).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.LeaveType).WithMany(x => x.LeaveRequests).HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReviewedByUserAccount).WithMany().HasForeignKey(x => x.ReviewedByUserAccountId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.Property(x => x.Email).HasMaxLength(255);
            entity.Property(x => x.NormalizedEmail).HasMaxLength(255);
            entity.Property(x => x.FullName).HasMaxLength(100);
            entity.Property(x => x.PasswordHash).HasMaxLength(512);
            entity.Property(x => x.Role).HasMaxLength(50);
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
            entity.HasIndex(x => x.EmployeeId).IsUnique().HasFilter("[EmployeeId] IS NOT NULL");
            entity.HasOne(x => x.Employee).WithOne().HasForeignKey<UserAccount>(x => x.EmployeeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(x => x.TokenHash).HasMaxLength(128);
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne(x => x.UserAccount).WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
            entity.Ignore(x => x.IsActive);
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.Property(x => x.Provider).HasMaxLength(50);
            entity.Property(x => x.ProviderSubject).HasMaxLength(255);
            entity.Property(x => x.ProviderEmail).HasMaxLength(255);
            entity.HasIndex(x => new { x.Provider, x.ProviderSubject }).IsUnique();
            entity.HasIndex(x => new { x.UserAccountId, x.Provider }).IsUnique();
            entity.HasOne(x => x.UserAccount).WithMany(x => x.ExternalLogins).HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.Property(x => x.TokenHash).HasMaxLength(128);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserAccountId, x.ExpiresAtUtc });
            entity.HasOne(x => x.UserAccount)
                .WithMany(x => x.PasswordResetTokens)
                .HasForeignKey(x => x.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void SeedReferenceData(ModelBuilder modelBuilder)
    {
        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Department>().HasData(
            new Department { Id = 1, Code = "ENG", Name = "Engineering", Description = "Product engineering and quality", CreatedAtUtc = seededAt },
            new Department { Id = 2, Code = "HR", Name = "Human Resources", Description = "People operations and culture", CreatedAtUtc = seededAt },
            new Department { Id = 3, Code = "FIN", Name = "Finance", Description = "Finance and accounting", CreatedAtUtc = seededAt },
            new Department { Id = 4, Code = "OPS", Name = "Operations", Description = "Business operations", CreatedAtUtc = seededAt });

        modelBuilder.Entity<LeaveType>().HasData(
            new LeaveType { Id = 1, Code = "PTO", Name = "Paid Time Off", Description = "Planned personal or vacation leave; entitlement is configurable.", AnnualEntitlement = 18, CarryForwardLimit = 10, MaxConsecutiveDays = 10, IsPaid = true, AllowsHalfDay = true, IsActive = true, SortOrder = 1 },
            new LeaveType { Id = 2, Code = "SICK", Name = "Sick Leave", Description = "Leave for illness or medical care.", AnnualEntitlement = 12, CarryForwardLimit = 0, DocumentRequiredAfterDays = 2, IsPaid = true, AllowsHalfDay = true, IsActive = true, SortOrder = 2 },
            new LeaveType { Id = 3, Code = "PARENTAL", Name = "Parental Leave", Description = "Parental leave governed by company policy and applicable law.", AnnualEntitlement = 0, CarryForwardLimit = 0, IsPaid = true, AllowsHalfDay = false, IsActive = true, SortOrder = 3 },
            new LeaveType { Id = 4, Code = "BEREAVEMENT", Name = "Bereavement Leave", Description = "Time away following the loss of a family member.", AnnualEntitlement = 5, CarryForwardLimit = 0, MaxConsecutiveDays = 5, IsPaid = true, AllowsHalfDay = false, IsActive = true, SortOrder = 4 },
            new LeaveType { Id = 5, Code = "VOLUNTEER", Name = "Volunteer Time Off", Description = "Paid time to support approved charitable activities.", AnnualEntitlement = 2, CarryForwardLimit = 0, MaxConsecutiveDays = 2, IsPaid = true, AllowsHalfDay = true, IsActive = true, SortOrder = 5 },
            new LeaveType { Id = 6, Code = "COMP_OFF", Name = "Compensatory Off", Description = "Time off granted for approved work on a holiday or weekend.", AnnualEntitlement = 0, CarryForwardLimit = 5, IsPaid = true, AllowsHalfDay = true, IsActive = true, SortOrder = 6 },
            new LeaveType { Id = 7, Code = "UNPAID", Name = "Unpaid Leave", Description = "Approved leave without pay after paid balances are exhausted.", AnnualEntitlement = 0, CarryForwardLimit = 0, IsPaid = false, AllowsHalfDay = true, IsActive = true, SortOrder = 7 });
    }
}
