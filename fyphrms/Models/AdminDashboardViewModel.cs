using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fyphrms.Models
{
    public class AdminDashboardViewModel
    {
        [Display(Name = "Total Employees")]
        public int TotalEmployees { get; set; }

        [Display(Name = "Total Departments")]
        public int TotalDepartments { get; set; }

        public List<EmployeeListItem> Employees { get; set; } = new List<EmployeeListItem>();
        public IEnumerable<UserListItem> SystemUsers { get; set; } = new List<UserListItem>();
    }

    public class EmployeeListItem
    {
        public int EmployeeID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Position { get; set; } 
        public string Department { get; set; } 
    }

    public class UserListItem
    {
        public string UserID { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string CurrentRole { get; set; } = "N/A"; 
    }

    public class EmployeeUserCreationViewModel
    {
        
        [Required(ErrorMessage = "Email is required for system account.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [Display(Name = "Company Email (Username)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required for system account.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Temporary Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty; 

        [Required(ErrorMessage = "System Role is required.")]
        [Display(Name = "System Role")]
        public string RoleName { get; set; } = "Employee"; 

        
        [Required(ErrorMessage = "First Name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid Contact Number.")]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; } 

        [Required(ErrorMessage = "Basic Salary is required.")]
        [Display(Name = "Basic Salary (RM)")]
        
        public decimal BasicSalary { get; set; }

        [Required(ErrorMessage = "Join Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Join Date")]
        public DateTime JoinDate { get; set; }

        [Required(ErrorMessage = "Employment Type is required.")]
        [Display(Name = "Employment Type")]
        public string EmploymentType { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "Department is required.")]
        [Display(Name = "Department")]
        public int DepartmentID { get; set; }

        [Required(ErrorMessage = "Position is required.")]
        [Display(Name = "Position/Job Title")]
        public int PositionID { get; set; }

        public string ICNumber { get; set; }

        public IFormFile ProfilePictureFile { get; set; }

        
        public IEnumerable<Department>? Departments { get; set; }
        public IEnumerable<Position>? Positions { get; set; }

        
        public IEnumerable<SelectListItem>? AvailableRoles { get; set; }
    }

    public class SimpleUserCreationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public string RoleName { get; set; } 
    }
}