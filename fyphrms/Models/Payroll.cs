// Payroll.cs
using System.ComponentModel.DataAnnotations;

namespace fyphrms.Models
{
    public class Payroll
    {
        [Key]
        public int PayrollID { get; set; }

        [Required]
        public int EmployeeID { get; set; }
        public Employee Employee { get; set; } = default!;

        public DateTime? PaymentDate { get; set; }
        public string? PaymentStatus { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        public decimal Allowances { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal Deductions { get; set; }

        public decimal EPFEmployee { get; set; }
        public decimal EPFEmployer { get; set; }

        public decimal SOCSOEmployee { get; set; }
        public decimal SOCSOEmployer { get; set; }

        public decimal EISEmployee { get; set; }
        public decimal EISEmployer { get; set; }
        public decimal PCB { get; set; } 

        [Required]
        public decimal NetSalary { get; set; }

        
    }
}