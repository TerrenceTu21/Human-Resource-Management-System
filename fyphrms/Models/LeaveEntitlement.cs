// LeaveEntitlement.cs 
using System.ComponentModel.DataAnnotations;

namespace fyphrms.Models
{
    public class LeaveEntitlement
    {
        [Key]
        public int EntitlementID { get; set; }

        [Required]
        public int EmployeeID { get; set; }
        public Employee Employee { get; set; } = default!;

        [Required]
        public int LeaveTypeID { get; set; }
        public LeaveType LeaveType { get; set; } = default!;

        [Required]
        public int Year { get; set; }

        [Required]
        public int TotalDays { get; set; }
    }
}