using HRConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRConnect.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(employee => employee.FirstName)
                .HasMaxLength(50);

            entity.Property(employee => employee.LastName)
                .HasMaxLength(50);

            entity.Property(employee => employee.Email)
                .HasMaxLength(255);

            entity.Property(employee => employee.Designation)
                .HasMaxLength(100);

            entity.HasIndex(employee => employee.Email)
                .IsUnique();
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.Property(user => user.Email)
                .HasMaxLength(255);

            entity.Property(user => user.NormalizedEmail)
                .HasMaxLength(255);

            entity.Property(user => user.FullName)
                .HasMaxLength(100);

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(512);

            entity.Property(user => user.Role)
                .HasMaxLength(50);

            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(refreshToken => refreshToken.TokenHash)
                .HasMaxLength(128);

            entity.Property(refreshToken => refreshToken.ReplacedByTokenHash)
                .HasMaxLength(128);

            entity.HasIndex(refreshToken => refreshToken.TokenHash)
                .IsUnique();

            entity.HasOne(refreshToken => refreshToken.UserAccount)
                .WithMany()
                .HasForeignKey(refreshToken => refreshToken.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(refreshToken => refreshToken.IsActive);
        });
    }
}
