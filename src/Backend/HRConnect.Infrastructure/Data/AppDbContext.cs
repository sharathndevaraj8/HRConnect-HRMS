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
    }
}
