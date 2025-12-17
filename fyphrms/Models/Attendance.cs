// Attendance.cs
using System.ComponentModel.DataAnnotations;

namespace fyphrms.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceID { get; set; }

        [Required]
        public int EmployeeID { get; set; }
        public Employee Employee { get; set; } = default!;

        [DataType(DataType.Date)]
        [Required]
        public DateTime Date { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? CheckInTime { get; set; } 

        [DataType(DataType.Time)]
        public TimeSpan? CheckOutTime { get; set; }
    }
}