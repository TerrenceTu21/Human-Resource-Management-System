using System;
using System.ComponentModel.DataAnnotations;

namespace fyphrms.Models.Shared
{
    public class MyProfileViewModel
    {

        public int EmployeeID { get; set; }
        public string UserID { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";

        
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;

        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string? Address { get; set; }

        [Display(Name = "IC/Passport Number")]
        public string? ICNumber { get; set; }

        
        [DataType(DataType.Date)]
        [Display(Name = "Join Date")]
        public DateTime JoinDate { get; set; }

        [Display(Name = "Employment Type")]
        public string EmploymentType { get; set; } = string.Empty; 

        
        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C}")]
        [Display(Name = "Basic Salary")]
        public decimal BasicSalary { get; set; }

        
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;

        
        [Display(Name = "Profile Picture")]
        public string? ProfilePicturePath { get; set; }
    }

    public class ContactUpdateDto
    {
        public int EmployeeID { get; set; }
        public string ContactNumber { get; set; }
        public string Address { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; }
    }

}