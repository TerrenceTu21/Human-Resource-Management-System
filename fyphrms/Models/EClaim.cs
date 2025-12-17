// EClaim.cs 
using System.ComponentModel.DataAnnotations;

namespace fyphrms.Models
{
    public class EClaim
    {
        [Key]
        public int ClaimID { get; set; }

        [Required]
        public int EmployeeID { get; set; }
        public Employee Employee { get; set; } = default!;

        [DataType(DataType.Date)]
        public DateTime ClaimDate { get; set; } = DateTime.UtcNow.Date;

        [DataType(DataType.Date)]
        public DateTime ExpensesDate { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public string? RejectReason { get; set; }

        
        public int? ApprovedBy { get; set; } 
        public Employee? Approver { get; set; } 

        
        public ICollection<ClaimDocument> ClaimDocuments { get; set; } = new List<ClaimDocument>();
    }
}