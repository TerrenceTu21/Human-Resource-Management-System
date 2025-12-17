using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace fyphrms.Models.Employees
{
    public class RecentClaimModel
    {
        public int ClaimID { get; set; }
        public string Title { get; set; } 
        public decimal Amount { get; set; }
        public string Status { get; set; } 
        public DateTime ClaimDate { get; set; }
    }

    public class EmployeeDashboardViewModel
    {
        // --- Welcome Banner Data ---
        public string FullName { get; set; }
        public string PositionTitle { get; set; } 

        // --- Leave Summary Data ---
        public List<LeaveBalanceViewModel> LeaveBalances { get; set; } = new List<LeaveBalanceViewModel>();

        // --- E-Claim Summary Data ---
        public List<RecentClaimModel> RecentClaims { get; set; } = new List<RecentClaimModel>();

        public List<Holiday> UpcomingHolidays { get; set; } = new List<Holiday>();
        public List<Holiday> AllHolidays { get; set; } = new List<Holiday>();
    }

    public class LeaveBalanceViewModel
    {
        public int LeaveTypeID { get; set; }
        public string LeaveType { get; set; }        
        public decimal TotalEntitlement { get; set; } 
        public decimal DaysTaken { get; set; }        
        public decimal DaysRemaining { get; set; }    
    }

    public class AvailableLeaveType
    {
        public int LeaveTypeID { get; set; }
        public string TypeName { get; set; }
        
        public decimal DaysRemaining { get; set; } 
    }

    public class EmployeeLeaveApplicationViewModel
    {
        public int SelectedLeaveTypeID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }

        public IFormFile? ProofPath { get; set; }

        public List<AvailableLeaveType> AvailableLeaveTypes { get; set; } = new List<AvailableLeaveType>();

        
        public List<LeaveBalanceViewModel> LeaveBalances { get; set; } = new List<LeaveBalanceViewModel>();
    }

    

    public class LeaveHistoryRecord
    {
        public int ID { get; set; }
        public string LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Days { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string RejectReason { get; set; }
    }

    public class EmployeeLeaveHistoryViewModel
    {
        public List<LeaveHistoryRecord> HistoryRecords { get; set; } = new List<LeaveHistoryRecord>();
    }

    public class PastExpenseDateAttribute : ValidationAttribute
    {
        private readonly int _maxDaysBack;

        public PastExpenseDateAttribute(int maxDaysBack)
        {
            _maxDaysBack = maxDaysBack;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime expenseDate)
            {
                DateTime cutoffDate = DateTime.Today.AddDays(-_maxDaysBack);

                if (expenseDate < cutoffDate)
                {
                    
                    return new ValidationResult(
                        $"Expense date cannot be older than {_maxDaysBack} days (must be on or after {cutoffDate:d MMM yyyy}).",
                        new[] { validationContext.MemberName }
                    );
                }
            }
            return ValidationResult.Success;
        }
    }

    public class EClaimApplicationViewModel
    {
        public EClaimApplicationViewModel()
        {
            DateOfExpense = DateTime.Today;
        }

        [Required(ErrorMessage = "The amount is required.")]
        [Range(0.01, 100000.00, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "A description (reason/type) for the claim is required.")]
        [StringLength(500)]
        public string Description { get; set; }

        [Required(ErrorMessage = "A date of expense for the claim is required.")]
        [PastExpenseDate(31, ErrorMessage = "Expense date cannot be more than 31 days in the past.")]
        public DateTime DateOfExpense { get; set; }

        [Required(ErrorMessage = "Please upload a receipt or supporting document.")]
        [Display(Name = "Upload Proof (Receipt/Invoice)")]
        
        public IFormFile DocumentFile { get; set; }
    }

    public class EClaimHistoryRecord
    {
        public int ID { get; set; }

        
        public string Type { get; set; }

        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        
        public DateTime ClaimDate { get; set; }
        public DateTime ExpensesDate { get; set; }

        
        public string ProofFileName { get; set; }

        
        public string ProofFilePath { get; set; }

        public string Status { get; set; }
    }

    public class EClaimHistoryViewModel
    {
        public List<EClaimHistoryRecord> HistoryRecords { get; set; } = new List<EClaimHistoryRecord>();
    }

    public class PayslipRecord
    {
        public int ID { get; set; }

        
        public string MonthYear { get; set; }

        [DataType(DataType.Currency)]
        public decimal NetPay { get; set; }

        
        public string Status { get; set; }
    }

    public class PayrollViewModel
    {
        // --- Current Month Summary ---
        public string CurrentMonthDisplay { get; set; }

        [DataType(DataType.Currency)]
        public decimal CurrentMonthNetPay { get; set; }

        public string CurrentMonthStatus { get; set; }
        public int CurrentMonthPayrollID { get; set; }

        // --- Previous Payslips ---
        public List<PayslipRecord> PreviousPayslips { get; set; } = new List<PayslipRecord>();
    }

    public class EmployeePayslipSummaryForEmployeeViewModel
    {
        public int PayrollID { get; set; } 
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthYearDisplay { get; set; } 
        public decimal NetPay { get; set; }
        public string Status { get; set; } 
    }

    public class EmployeePayrollViewModel
    {
        public string CurrentMonthYearDisplay { get; set; } 
        public decimal CurrentMonthNetPay { get; set; }
        public string CurrentMonthStatus { get; set; } 
        public int CurrentMonthPayrollID { get; set; } 

        public List<EmployeePayslipSummaryForEmployeeViewModel> PreviousPayslips { get; set; } = new List<EmployeePayslipSummaryForEmployeeViewModel>();
    }

    public class EmployeePayslipDetailViewModel
    {
        // Payslip Period
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthYearDisplay => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
        public DateTime? PaymentDate { get; set; }

        // Employee Details
        public string EmployeeName { get; set; }
        public string EmployeeIC { get; set; }
        public string EmployeeDepartment { get; set; }
        public string EmployeePosition { get; set; }

        // Financial Details
        public decimal BasicSalary { get; set; } 
        public decimal Allowance { get; set; } 
        public decimal GrossSalary => BasicSalary + Allowance;

        // Statutory Deductions (Matches Payrolls table)
        public decimal EPFEmployee { get; set; }
        public decimal EPFEmployer { get; set; }
        public decimal SOCSOEmployee { get; set; }
        public decimal SOCSOEmployer { get; set; }
        public decimal EISEmployee { get; set; }
        public decimal EISEmployer { get; set; }
        public decimal PCB { get; set; }

        // Other Deductions
        public decimal OtherDeductions { get; set; } 
        public string OtherDeductionDescription { get; set; }

        // Summary
        public decimal TotalDeductions => EPFEmployee  + SOCSOEmployee  + EISEmployee + PCB + OtherDeductions;
        public decimal NetSalary { get; set; }
        public string Status { get; set; }
    }

    public class AttendanceViewModel
    {
        
        public DateTime? LastClockIn { get; set; }

        
        public DateTime? LastClockOut { get; set; }

        
        public bool IsClockedIn { get; set; }
    }

    public class FaceVerificationRequest
    {
        [Required(ErrorMessage = "Image data is required for verification.")]
        public string ImageData { get; set; } 

    }
}