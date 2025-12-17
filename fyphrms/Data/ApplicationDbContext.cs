using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using fyphrms.Models; // Ensure this namespace matches your models

namespace fyphrms.Data
{
    // IdentityDbContext<ApplicationUser> includes all standard Identity tables (AspNetUsers, AspNetRoles, etc.)
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ------------------------------------
        // HRMS EntitySets (DbSets) - Mapped to Tables
        // ------------------------------------

        // Primary HRMS Entities
        public DbSet<Employee> Employees { get; set; } = default!;
        public DbSet<Department> Departments { get; set; } = default!;
        public DbSet<Position> Positions { get; set; } = default!; // NEW

        // Attendance & Leave
        public DbSet<Attendance> Attendance { get; set; } = default!;
        public DbSet<Leave> Leaves { get; set; } = default!;
        public DbSet<LeaveType> LeaveTypes { get; set; } = default!;
        public DbSet<LeaveEntitlement> LeaveEntitlements { get; set; } = default!; // NEW


        // Claims & Payroll
        public DbSet<EClaim> Claims { get; set; } = default!; // NEW
        public DbSet<ClaimDocument> ClaimDocuments { get; set; } = default!; // NEW
        public DbSet<Payroll> Payrolls { get; set; } = default!;

        public DbSet<Holiday> Holidays { get; set; }


        // ------------------------------------
        // Configure Relationships (OnModelCreating)
        // ------------------------------------
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // MUST be the first call to configure Identity tables
            base.OnModelCreating(modelBuilder);

            // 1. Enforce unique constraint on the Email column
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            // 2. Enforce unique constraint on the ContactNumber column
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.ContactNumber)
                .IsUnique();

            // --- 1. Employee <--> Identity User (One-to-One) ---
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.UserAccount) // Navigation property in Employee
                .WithOne()
                .HasForeignKey<Employee>(e => e.UserID)
                .IsRequired();


            // --- 2. Employee <--> Department (Many-to-One) ---
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentID);


            // --- 3. Employee <--> Position (Many-to-One) ---
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Position)
                .WithMany(p => p.Employees)
                .HasForeignKey(e => e.PositionID);

            modelBuilder.Entity<Position>()
                .HasOne(p => p.Department)          // A Position has one Department
                .WithMany(d => d.Positions)         // A Department has many Positions
                .HasForeignKey(p => p.DepartmentID) // Use DepartmentID as the FK
                .OnDelete(DeleteBehavior.Restrict);  // Optional: prevent cascading delete


            // --- 4. Leave (ApprovedBy) <--> Employee (Many-to-One, Self-Reference) ---
            modelBuilder.Entity<Leave>()
                .HasOne(l => l.Approver) // The Employee who approved the leave
                .WithMany(e => e.ApprovedLeaves) // The collection of leaves approved by this Employee
                .HasForeignKey(l => l.ApprovedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete if an approver leaves


            // --- 5. EClaim (ApprovedBy) <--> Employee (Many-to-One, Self-Reference) ---
            modelBuilder.Entity<EClaim>()
                .HasOne(c => c.Approver) // The Employee who approved the claim
                .WithMany(e => e.ApprovedClaims) // The collection of claims approved by this Employee
                .HasForeignKey(c => c.ApprovedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);


            // --- 6. EClaim <--> ClaimDocument (One-to-Many) ---
            modelBuilder.Entity<ClaimDocument>()
                .HasOne(cd => cd.Claim)
                .WithMany(c => c.ClaimDocuments)
                .HasForeignKey(cd => cd.ClaimID);


            // --- 7. LeaveEntitlement <--> Employee & LeaveType (Composite Foreign Key) ---
            // Entity Framework handles the composite key implicitly if the properties are defined correctly
            modelBuilder.Entity<LeaveEntitlement>()
                .HasKey(le => le.EntitlementID); // Explicitly define the primary key

            modelBuilder.Entity<Holiday>()
                .Property(h => h.Date)
                .HasColumnType("date");
        }
    }
}