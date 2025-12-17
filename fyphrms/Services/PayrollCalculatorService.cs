using fyphrms.Data;
using fyphrms.Models;
using fyphrms.Models.HR;
using Microsoft.EntityFrameworkCore; 


public class PayrollCalculatorService
{
    private readonly ApplicationDbContext _context;

    public PayrollCalculatorService(ApplicationDbContext context)
    {
        _context = context;
    }

    private const decimal EPF_EMPLOYEE_RATE = 0.11m; // 11% 
    private const decimal EPF_EMPLOYER_RATE = 0.13m; // 11% 
    private const decimal SOCSO_RATE = 0.005m;      // 0.5% 
    private const decimal EIS_RATE = 0.002m;        // 0.2% 

    private const decimal PCB_SIMPLIFIED_RATE = 0.01m; 

    private const decimal FIXED_DEDUCTIONS = 50.00m;
    private const decimal FIXED_ALLOWANCES = 150.00m;

    public Payroll CalculatePayslip(Employee employee, int month, int year)
    {
        decimal basicSalary = employee.BasicSalary;

        
        var unpaidLeaveDays = _context.Leaves
            .Where(l => l.EmployeeID == employee.EmployeeID
                        && l.StartDate.Month == month
                        && l.StartDate.Year == year
                        && l.Status == "Approved"
                        && l.LeaveType.TypeName == "Unpaid Leave")
            .AsEnumerable() 
            .Sum(l => (l.EndDate - l.StartDate).Days + 1); 

        int workingDaysInMonth = DateTime.DaysInMonth(year, month);
        decimal unpaidDeduction = (basicSalary / workingDaysInMonth) * unpaidLeaveDays;

        decimal salaryAfterLeave = basicSalary - unpaidDeduction;

        
        decimal epfEmployee = salaryAfterLeave * EPF_EMPLOYEE_RATE;
        decimal socsoEmployee = salaryAfterLeave * SOCSO_RATE;
        decimal eisEmployee = salaryAfterLeave * EIS_RATE;
        decimal pcb = salaryAfterLeave * PCB_SIMPLIFIED_RATE;

        
        decimal epfEmployer = salaryAfterLeave * EPF_EMPLOYER_RATE;
        decimal socsoEmployer = salaryAfterLeave * SOCSO_RATE;
        decimal eisEmployer = salaryAfterLeave * EIS_RATE;

        decimal allowances = FIXED_ALLOWANCES;
        decimal otherDeductions = FIXED_DEDUCTIONS + unpaidDeduction;

        decimal totalDeductions = epfEmployee + socsoEmployee + eisEmployee + pcb + FIXED_DEDUCTIONS + unpaidDeduction;
        decimal netSalary = salaryAfterLeave + allowances - (epfEmployee + socsoEmployee + eisEmployee + pcb + FIXED_DEDUCTIONS);

        return new Payroll
        {
            EmployeeID = employee.EmployeeID,
            Allowances = allowances,
            Deductions = unpaidDeduction + FIXED_DEDUCTIONS, 
            EPFEmployee = epfEmployee,
            EPFEmployer = epfEmployer,
            SOCSOEmployee = socsoEmployee,
            SOCSOEmployer = socsoEmployer,
            EISEmployee = eisEmployee,
            EISEmployer = eisEmployer,
            PCB = pcb,
            NetSalary = netSalary
        };
    }

    
}