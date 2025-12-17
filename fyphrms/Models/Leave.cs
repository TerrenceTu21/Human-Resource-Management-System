// Leave.cs
using System.ComponentModel.DataAnnotations;

namespace fyphrms.Models
{
    public class Leave
    {
        [Key]
        public int LeaveID { get; set; }

        [Required]
        public int EmployeeID { get; set; }
        public Employee Employee { get; set; } = default!;

        [Required]
        public int LeaveTypeID { get; set; }
        public LeaveType LeaveType { get; set; } = default!;

        [DataType(DataType.Date)]
        [Required]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        [Required]
        public DateTime EndDate { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public string? RejectReason {  get; set; } = string.Empty;

        public string? ProofPath { get; set; } = null;


        
        public int? ApprovedBy { get; set; } 
        public Employee? Approver { get; set; } 
    }
}