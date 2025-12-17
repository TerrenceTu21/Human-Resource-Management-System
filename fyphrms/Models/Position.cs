// Position.cs 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fyphrms.Models
{
    public class Position
    {
        [Key]
        public int PositionID { get; set; }

        [Required]
        public string PositionTitle { get; set; } = string.Empty;

        public string? JobDescription { get; set; }

        [Required]
        public int? DepartmentID { get; set; } 

        [ForeignKey("DepartmentID")]
        public Department? Department { get; set; } = default!; 

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}