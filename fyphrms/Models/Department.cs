// Department.cs
using System.ComponentModel.DataAnnotations;

namespace fyphrms.Models
{
    public class Department
    {
        [Key]
        public int DepartmentID { get; set; }

        [Required]
        public string DepartmentName { get; set; } = string.Empty;

        public ICollection<Position> Positions { get; set; } = new List<Position>();

        
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}