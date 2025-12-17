using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace fyphrms.Models
{
    public class Holiday
    {
        [Key]
        public int HolidayID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        public bool IsPublicHoliday { get; set; } = true; 
    }
}
