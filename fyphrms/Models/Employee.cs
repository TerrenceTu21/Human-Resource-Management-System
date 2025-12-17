// Employee.cs
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

namespace fyphrms.Models
{
    [Index(nameof(Email), IsUnique = true)] 
    [Index(nameof(ContactNumber), IsUnique = true)]
    public class Employee
    {
        [Key]
        public int EmployeeID { get; set; }

        
        [Required]
        public string UserID { get; set; } = string.Empty;
        public ApplicationUser UserAccount { get; set; } = default!;

        // --- Core Details ---
        [Required]
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;
        public string? ContactNumber { get; set; }

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Address { get; set; }
        public decimal BasicSalary { get; set; }

        [DataType(DataType.Date)]
        public DateTime JoinDate { get; set; }

        public string? ICNumber { get; set; }

        public string? ProfilePicturePath { get; set; }
        public bool IsActive { get; set; } = true;

        public string EmploymentType { get; set; } = string.Empty; 

        
        public int DepartmentID { get; set; }
        public Department Department { get; set; } = default!;

        public int PositionID { get; set; }
        public Position Position { get; set; } = default!;

        
        public ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
        public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
        public ICollection<LeaveEntitlement> Entitlements { get; set; } = new List<LeaveEntitlement>();
        public ICollection<EClaim> Claims { get; set; } = new List<EClaim>();
        public ICollection<Payroll> PayrollRecords { get; set; } = new List<Payroll>();

        
        public ICollection<Leave> ApprovedLeaves { get; set; } = new List<Leave>();
        public ICollection<EClaim> ApprovedClaims { get; set; } = new List<EClaim>();
    }
}