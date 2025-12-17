// LeaveType.cs
using System.ComponentModel.DataAnnotations;

namespace fyphrms.Models
{
    public class LeaveType
    {
        [Key]
        public int LeaveTypeID { get; set; }

        [Required]
        public string TypeName { get; set; } = string.Empty;

        public string? Description { get; set; }
        public bool RequiresProof { get; set; } = false;

        public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
        public ICollection<LeaveEntitlement> Entitlements { get; set; } = new List<LeaveEntitlement>();
    }
}