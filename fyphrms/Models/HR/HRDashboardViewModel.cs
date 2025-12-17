using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace fyphrms.Models.HR
{

    public class HRDashboardViewModel
    {
        
        public string UserName { get; set; }
        public int? TotalEmployees { get; set; }
        public int? PendingLeaves { get; set; }
        public int? PendingClaims { get; set; }
        public List<Holiday> UpcomingHolidays { get; set; } = new List<Holiday>();
    }
    public class HREmployeeViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
        public DateTime DateJoined { get; set; }
        public bool IsActive { get; set; }
    }

    
    public class HREmployeeListViewModel
    {
        public List<HREmployeeViewModel> Employees { get; set; }
    }

    public class EmployeeSupabaseModel
    {
        
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("date_joined")]
        public DateTime DateJoined { get; set; }

        [JsonPropertyName("position_id")]
        public string PositionId { get; set; }

        [JsonPropertyName("department_id")]
        public string DepartmentId { get; set; }
    }

    public class HREditEmployeeViewModel
    {
        public int EmployeeID { get; set; }


        [Display(Name = "First Name")]
        public string FirstName { get; set; }


        [Display(Name = "Last Name")]
        public string LastName { get; set; }


        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }


        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }


        [Display(Name = "Department")]
        public int DepartmentID { get; set; }


        [Display(Name = "Position")]
        public int PositionID { get; set; }
        [Display(Name = "Basic Salary")]
        public decimal BasicSalary { get; set; }

        
        [Display(Name = "Employment Type")]
        public string EmploymentType { get; set; }

        
        public SelectList Departments { get; set; }
        public SelectList Positions { get; set; }
    }

    public class PositionSupabaseModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class DepartmentSupabaseModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class EmployeeDetailsViewModel
    {
        // HR Record Details
        public int EmployeeID { get; set; }
        public string UserID { get; set; } 

        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "IC/ID Number")] 
        public string? ICNumber { get; set; }

        [Display(Name = "Profile Picture Path")] 
        public string? ProfilePicturePath { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        public string Gender { get; set; }
        public string Address { get; set; }

        // Employment Details
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Basic Salary")]
        public decimal BasicSalary { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Join Date")]
        public DateTime JoinDate { get; set; }

        [Display(Name = "Employment Type")]
        public string EmploymentType { get; set; } 

        
        [Display(Name = "Department")]
        public string DepartmentName { get; set; }

        [Display(Name = "Position")]
        public string PositionTitle { get; set; }
    }

    public class LeaveRequestViewModel
    {
        public int ID { get; set; }
        public string EmployeeFullName { get; set; }
        public string LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Days { get; set; }
        public string ReasonSummary { get; set; }
        public string? RejectReason { get; set; }
        public string Status { get; set; }
        
        public int EmployeeID { get; set; }
        public string? ProofPath { get; set; }
    }

    // Main ViewModel for the HR Leave Management page
    public class HRLeaveManagementViewModel
    {
        public IEnumerable<LeaveRequestViewModel> LeaveRequests { get; set; } = new List<LeaveRequestViewModel>();
        public string ActiveFilter { get; set; } 
    }

    // Claim Request ViewModel for the list
    public class HRClaimViewModel
    {
        public int ID { get; set; }
        public string EmployeeName { get; set; }
        public string ClaimDescription { get; set; } 
        public decimal Amount { get; set; }
        public DateTime ClaimDate { get; set; }
        public DateTime DateOfExpenses { get; set; }
        public string Status { get; set; }
        public string? RejectReason { get; set; }
        public string ClaimDocumentFileName { get; set; }
        public string ProofDownloadUrl { get; set; }
    }

    // Main Management ViewModel
    public class HRClaimManagementViewModel
    {
        public List<HRClaimViewModel> Claims { get; set; } = new List<HRClaimViewModel>();

        
        public string ActiveFilter { get; set; }
    }

    
    public class PayrollMonthSummaryViewModel
    {
        public string GroupID => $"{Year}-{Month}";
        public string MonthYearDisplay { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal TotalNetPay { get; set; }

        
        public string Status { get; set; }
    }

    
    public class HRPayrollViewModel
    {
        public List<MonthlyPayrollSummary> MonthlySummaries { get; set; } = new List<MonthlyPayrollSummary>();
    }

    
    public class EmployeePayslipSummaryViewModel
    {
        public int PayrollID { get; set; }
        public int EmployeeID { get; set; } 
        public int Month { get; set; } 
        public int Year { get; set; } 

        public decimal BasicSalary { get; set; }
        public decimal EPFEmployee { get; set; }
        public decimal EPFEmployer { get; set; }
        public decimal SOCSOEmployee { get; set; }
        public decimal SOCSOEmployer { get; set; }
        public decimal EISEmployee { get; set; }
        public decimal EISEmployer { get; set; }
        public decimal PCB { get; set; }
        public decimal Deductions { get; set; }
        public decimal Allowances { get; set; }


        public string EmployeeName { get; set; }
        public string EmployeeIDDisplay { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal NetSalary { get; set; }

        
        public string Status { get; set; }
        public bool IsSimulated { get; set; } 
    }
    public class MonthlyPayrollSummary
    {
        public string GroupID { get; set; } = string.Empty; 
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalNetPay { get; set; }
        public string Status { get; set; } = "Pending"; 

        
        public string MonthYearDisplay => $"{Month:D2}/{Year}";
    }

    public class ViewPayslipDetailViewModel
    {
        public Payroll Payroll { get; set; } = default!;
        public Employee Employee { get; set; } = default!;
        public bool IsPreview { get; set; } = false; 
    }

    
    public class HRViewPayslipViewModel
    {
        public string MonthYearDisplay { get; set; }

        public List<EmployeePayslipSummaryViewModel> EmployeePayslips { get; set; } = new List<EmployeePayslipSummaryViewModel>();
    }

    

    public class DepartmentPositionViewModel
    {
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public List<PositionItem> Positions { get; set; } = new List<PositionItem>();
    }

    public class PositionItem
    {
        public int PositionID { get; set; }
        public string PositionTitle { get; set; } = string.Empty;
    }

    public class DepartmentsPositionsPageViewModel
    {
        public List<DepartmentPositionViewModel> Departments { get; set; } = new List<DepartmentPositionViewModel>();
    }

    public class AddDepartmentViewModel
    {
        [Required(ErrorMessage = "Department name is required")]
        [StringLength(100, ErrorMessage = "Department name must be at most 100 characters")]
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class HRAttendanceViewModel
    {
        public DateTime SelectedDate { get; set; }
        public List<Attendance> AttendanceList { get; set; } = new();
        public List<Employee> AllEmployees { get; set; } = new();

        // Search
        public string SearchQuery { get; set; } = "";

        // Summary
        public int PresentCount { get; set; }
        public int LateCount { get; set; }
        public int AbsentCount { get; set; }
    }


    public class AttendanceRecordVM
    {
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; } = "";
        public string Department { get; set; } = "";

        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }

        public string Status
        {
            get
            {
                if (CheckIn == null)
                    return "Absent";
                return "Present";
            }
        }
    }

    public class HRReportViewModel
    {
        public string SelectedReport { get; set; } = "Attendance";

        public int SelectedMonth { get; set; }
        public int SelectedYear { get; set; }

        public List<AttendanceAnalyticsModel> AttendanceData { get; set; } = new();
        public List<LeaveAnalyticsModel> LeaveData { get; set; } = new();
        public List<EClaimAnalyticsModel> EClaimData { get; set; } = new(); 
    }

    public class AttendanceAnalyticsModel
    {
        public string Month { get; set; } = string.Empty;
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
    }

    public class LeaveAnalyticsModel
    {
        public string LeaveType { get; set; } = string.Empty;
        public int TotalTaken { get; set; }
    }

    public class EClaimAnalyticsModel
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

}