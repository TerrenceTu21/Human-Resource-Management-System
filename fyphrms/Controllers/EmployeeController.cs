using fyphrms.Data;
using fyphrms.Models; 
using fyphrms.Models.Employees;
using fyphrms.Models.HR;
using fyphrms.Services;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace fyphrms.Controllers
{
    [Authorize(Roles = "Employee")] 
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EmployeeController> _logger;
        private readonly HttpClient _httpClient; 
        private readonly SupabaseConfig _supabaseConfig;
        private readonly IFaceRecognitionService _faceRecognitionService;


        public EmployeeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<EmployeeController> logger,
            HttpClient httpClient, 
            SupabaseConfig supabaseConfig,
            IFaceRecognitionService faceRecognitionService) 

        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _httpClient = httpClient;
            _supabaseConfig = supabaseConfig;
            _faceRecognitionService = faceRecognitionService;
        }

        //GET: /Employee/Index (Dashboard) 
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var employeeRecord = await _context.Employees
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserID == userId);

            if (employeeRecord == null)
            {
                TempData["ErrorMessage"] = "Your employee record could not be found. Please contact HR.";
                return View(new EmployeeDashboardViewModel()); 
            }

            var currentYear = DateTime.Now.Year;
            var employeeId = employeeRecord.EmployeeID;

            var entitlements = await _context.LeaveEntitlements
                .Where(le => le.EmployeeID == employeeId && le.Year == currentYear)
                .Include(le => le.LeaveType)
                .ToListAsync();

            var leavesAppliedList = await _context.Leaves
                .Where(l => l.EmployeeID == employeeId &&
                            l.StartDate.Year == currentYear &&
                            (l.Status == "Approved" || l.Status == "Pending"))
                .ToListAsync(); 

            var leavesTakenGrouped = leavesAppliedList
                .GroupBy(l => l.LeaveTypeID)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(l => (decimal)(l.EndDate.Date - l.StartDate.Date).TotalDays + 1)
                );

            var leaveBalances = new List<LeaveBalanceViewModel>();

            foreach (var entitlement in entitlements)
            {
                var daysTaken = leavesTakenGrouped.GetValueOrDefault(entitlement.LeaveTypeID, 0m); 

                var daysRemaining = entitlement.TotalDays - daysTaken;
                if (daysRemaining < 0) daysRemaining = 0m;

                leaveBalances.Add(new LeaveBalanceViewModel
                {
                    LeaveType = entitlement.LeaveType?.TypeName ?? "Unknown Leave Type",
                    TotalEntitlement = entitlement.TotalDays,
                    DaysTaken = daysTaken,
                    DaysRemaining = daysRemaining
                });
            }

            var recentClaims = await _context.Claims 
                .Where(c => c.EmployeeID == employeeRecord.EmployeeID)
                .OrderByDescending(c => c.ClaimDate)
                .Take(3)
                .Select(c => new RecentClaimModel
                {
                    Title = c.Description,
                    Amount = c.Amount,
                    ClaimDate = c.ClaimDate.Date,
                    Status = c.Status
                })
                .ToListAsync();

            // Fetch upcoming 3 holidays
            var today = DateOnly.FromDateTime(DateTime.Now);
            var upcomingHolidays = await _context.Holidays
                .Where(h => h.Date >= today)
                .OrderBy(h => h.Date)
                .Take(3)
                .ToListAsync();

            var allHolidays = await _context.Holidays
                .OrderBy(h => h.Date)
                .ToListAsync();

            var viewModel = new EmployeeDashboardViewModel
            {
                FullName = $"{employeeRecord.FirstName} {employeeRecord.LastName}",
                PositionTitle = employeeRecord.Position?.PositionTitle ?? "N/A",

                LeaveBalances = leaveBalances,

                RecentClaims = recentClaims,

                UpcomingHolidays = upcomingHolidays,
                AllHolidays = allHolidays
            };

            return View(viewModel);
        }

        //GET: Employee/LeaveApplication
        [HttpGet]
        public async Task<IActionResult> EmployeeLeaveApplication()
        {
            var userId = _userManager.GetUserId(User);
            var currentYear = DateTime.Now.Year;

            var employeeRecord = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserID == userId);

            if (employeeRecord == null)
            {
                TempData["ErrorMessage"] = "Employee record not found.";
                return RedirectToAction("Index");
            }

            var employeeId = employeeRecord.EmployeeID;

            var allLeaveTypes = await _context.LeaveTypes.ToListAsync();

            var entitlements = await _context.LeaveEntitlements
                .Where(le => le.EmployeeID == employeeId && le.Year == currentYear)
                .Include(le => le.LeaveType)
                .ToListAsync();

            var leavesAppliedList = await _context.Leaves
                .Where(l => l.EmployeeID == employeeId &&
                            l.StartDate.Year == currentYear &&
                            (l.Status == "Approved" || l.Status == "Pending"))
                .ToListAsync();

            var leavesTakenGrouped = leavesAppliedList
                .GroupBy(l => l.LeaveTypeID)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(l => (decimal)(l.EndDate.Date - l.StartDate.Date).TotalDays + 1)
                );

            var leaveBalances = new List<LeaveBalanceViewModel>();

            foreach (var entitlement in entitlements)
            {
                var daysTaken = leavesTakenGrouped.GetValueOrDefault(entitlement.LeaveTypeID, 0m);
                var daysRemaining = entitlement.TotalDays - daysTaken;

                leaveBalances.Add(new LeaveBalanceViewModel
                {
                    LeaveType = entitlement.LeaveType?.TypeName ?? "Unknown Leave Type", 
                    TotalEntitlement = entitlement.TotalDays,
                    DaysTaken = daysTaken,
                    DaysRemaining = Math.Max(0m, daysRemaining)
                });
            }
            
            var viewModel = new EmployeeLeaveApplicationViewModel
            {
                LeaveBalances = leaveBalances,

                AvailableLeaveTypes = allLeaveTypes.Select(lt => new AvailableLeaveType
                {
                    LeaveTypeID = lt.LeaveTypeID, 
                    TypeName = lt.TypeName,
                }).ToList(),

                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            };

            return View(viewModel);
        }

        //POST: Employee/SubmitLeave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitLeave(EmployeeLeaveApplicationViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var currentYear = DateTime.Now.Year;

            var employeeRecord = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserID == userId);

            if (employeeRecord == null)
            {
                TempData["ErrorMessage"] = "Employee record not found. Cannot submit leave.";
                return RedirectToAction("Index");
            }

            var employeeId = employeeRecord.EmployeeID;

            var allLeaveTypes = await _context.LeaveTypes.ToListAsync();
            model.AvailableLeaveTypes = allLeaveTypes.Select(lt => new AvailableLeaveType
            {
                LeaveTypeID = lt.LeaveTypeID,
                TypeName = lt.TypeName,
            }).ToList();

            var entitlements = await _context.LeaveEntitlements
                .Where(le => le.EmployeeID == employeeId && le.Year == currentYear)
                .Include(le => le.LeaveType)
                .ToListAsync();

            var leavesAppliedList = await _context.Leaves
                .Where(l => l.EmployeeID == employeeId &&
                            l.StartDate.Year == currentYear &&
                            (l.Status == "Approved" || l.Status == "Pending"))
                .ToListAsync();

            var leavesTakenGrouped = leavesAppliedList
                .GroupBy(l => l.LeaveTypeID)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(l => (decimal)(l.EndDate.Date - l.StartDate.Date).TotalDays + 1)
                );

            var leaveBalances = new List<LeaveBalanceViewModel>();
            foreach (var entitlement in entitlements)
            {
                var daysTaken = leavesTakenGrouped.GetValueOrDefault(entitlement.LeaveTypeID, 0m);
                var daysRemaining = entitlement.TotalDays - daysTaken;

                leaveBalances.Add(new LeaveBalanceViewModel
                {
                    LeaveTypeID = entitlement.LeaveTypeID,
                    LeaveType = entitlement.LeaveType?.TypeName ?? "Unknown Leave Type",
                    TotalEntitlement = entitlement.TotalDays,
                    DaysTaken = daysTaken,
                    DaysRemaining = Math.Max(0m, daysRemaining)
                });
            }

            model.LeaveBalances = leaveBalances;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form and try again.";
                return View("EmployeeLeaveApplication", model);
            }

            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "End Date cannot be before Start Date.");
                TempData["ErrorMessage"] = "End Date cannot be before Start Date.";
                return View("EmployeeLeaveApplication", model);
            }

            var requestedDays = (decimal)(model.EndDate.Date - model.StartDate.Date).TotalDays + 1;

            var unpaidLeaveType = await _context.LeaveTypes
                .FirstOrDefaultAsync(lt => lt.TypeName == "Unpaid Leave");

            int unpaidLeaveTypeID = unpaidLeaveType?.LeaveTypeID ?? 0;


            var relevantBalance = model.LeaveBalances
                .FirstOrDefault(b => b.LeaveTypeID == model.SelectedLeaveTypeID);

            if (model.SelectedLeaveTypeID != unpaidLeaveTypeID)
            {
                if (relevantBalance == null)
                {
                    ModelState.AddModelError(nameof(model.SelectedLeaveTypeID), "You do not have a registered entitlement for the selected leave type.");
                    TempData["ErrorMessage"] = "You do not have a registered entitlement for the selected leave type.";
                    return View("EmployeeLeaveApplication", model);
                }

                if (requestedDays > relevantBalance.DaysRemaining)
                {
                    ModelState.AddModelError(nameof(model.SelectedLeaveTypeID),
                        $"The requested {requestedDays} days exceeds your remaining balance of {relevantBalance.DaysRemaining} days for this leave type.");
                    TempData["ErrorMessage"] = "The requested leave period exceeds your remaining balance for this leave type.";
                    return View("EmployeeLeaveApplication", model);
                }
            }


            try
            {
                DateTime utcStartDate = DateTime.SpecifyKind(model.StartDate.Date, DateTimeKind.Utc);
                DateTime utcEndDate = DateTime.SpecifyKind(model.EndDate.Date, DateTimeKind.Utc);

                var newLeave = new Leave
                {
                    EmployeeID = employeeRecord.EmployeeID,
                    LeaveTypeID = model.SelectedLeaveTypeID,
                    StartDate = utcStartDate,
                    EndDate = utcEndDate,
                    Reason = model.Reason,
                    Status = "Pending",
                };

                var selectedLeaveType = await _context.LeaveTypes
                    .Where(l => l.LeaveTypeID == model.SelectedLeaveTypeID)
                    .Select(l => l.TypeName)
                    .FirstOrDefaultAsync();

                bool proofRequired = selectedLeaveType == "Sick Leave"
                                     || selectedLeaveType == "Hospitalization Leave";

                if (proofRequired)
                {
                    if (model.ProofPath == null)
                    {
                        ModelState.AddModelError("ProofFileUrl", $"{selectedLeaveType} requires proof document.");
                        TempData["ErrorMessage"] = $"{selectedLeaveType} requires proof document.";
                        return View("EmployeeLeaveApplication", model);
                    }

                    newLeave.ProofPath = await UploadFileToSupabaseStorage(model.ProofPath, "leaveproof");
                }

                _context.Leaves.Add(newLeave);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Leave application submitted successfully and is now pending approval.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred while submitting your leave. Please try again.";
                return View("EmployeeLeaveApplication", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeLeaveHistory()
        {
            var userId = _userManager.GetUserId(User);

            var employeeRecord = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserID == userId);

            if (employeeRecord == null)
            {
                TempData["ErrorMessage"] = "Employee record not found.";
                return RedirectToAction("Index");
            }

            var leaveHistory = await _context.Leaves
                .Where(l => l.EmployeeID == employeeRecord.EmployeeID)
                .Include(l => l.LeaveType) 
                .OrderByDescending(l => l.StartDate)
                .Select(l => new LeaveHistoryRecord
                {
                    ID = l.LeaveID,
                    LeaveType = l.LeaveType.TypeName,
                    StartDate = l.StartDate.Date,
                    EndDate = l.EndDate.Date,
                    Days = (int)(l.EndDate.Date - l.StartDate.Date).TotalDays + 1,
                    Reason = l.Reason,
                    RejectReason = l.RejectReason,
                    Status = l.Status
                })
                .ToListAsync();

            var viewModel = new EmployeeLeaveHistoryViewModel
            {
                HistoryRecords = leaveHistory
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEClaim(EClaimApplicationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("EmployeeEClaimApplication", model);
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                var employeeRecord = await _context.Employees.FirstOrDefaultAsync(e => e.UserID == userId);

                if (employeeRecord == null)
                {
                    TempData["ErrorMessage"] = "Error: Employee record not found.";
                    return RedirectToAction("Index");
                }

                string s3FilePath = await UploadFileToSupabaseStorage(model.DocumentFile, userId);
                if (string.IsNullOrEmpty(s3FilePath))
                {
                    TempData["ErrorMessage"] = "Failed to upload document proof. Please try again.";
                    return View("EmployeeEClaimApplication", model);
                }

                var newClaim = new EClaim
                {
                    EmployeeID = employeeRecord.EmployeeID,
                    ClaimDate = DateTime.Now.ToUniversalTime(),
                    Amount = model.Amount,
                    ExpensesDate = DateTime.SpecifyKind(model.DateOfExpense.Date, DateTimeKind.Utc),
                    Description = model.Description,
                    Status = "Pending",
                    ApprovedBy = null 
                };

                _context.Claims.Add(newClaim);
                await _context.SaveChangesAsync(); 

                var claimDocument = new ClaimDocument
                {
                    ClaimID = newClaim.ClaimID,
                    FilePath = s3FilePath,
                    FileName = model.DocumentFile.FileName
                };

                _context.ClaimDocuments.Add(claimDocument);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "E-Claim submitted successfully and document uploaded!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while submitting the claim: {ex.Message}";
                return View("EmployeeEClaimApplication", model);
            }
        }

        [HttpGet]
        public IActionResult EmployeeEClaimApplication()
        {
            return View(new EClaimApplicationViewModel());
        }

        private async Task<string> UploadFileToSupabaseStorage(IFormFile file, string userId)
        {
            var bucketName = "claim-document";

            var extension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var pathInBucket = $"proofs/{userId}/{uniqueFileName}";

            var uploadUrl = $"{_supabaseConfig.StorageUrl}/object/{bucketName}/{pathInBucket}";

            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseConfig.ServiceKey}");
                using var content = new MultipartFormDataContent();

                var fileStreamContent = new StreamContent(file.OpenReadStream());
                fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

                content.Add(fileStreamContent, name: "file", fileName: file.FileName);

                var response = await _httpClient.PostAsync(uploadUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var publicUrl = $"{_supabaseConfig.StorageUrl}/object/public/{bucketName}/{pathInBucket}";

                    return publicUrl;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase Upload failed. Status: {Status}. Response: {Error}", response.StatusCode, errorContent);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HttpClient Upload failed for user {UserId}", userId);
                return null;
            }
        }

        private TimeZoneInfo GetMalaysiaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuala_Lumpur");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeEClaimHistory()
        {
            var userId = _userManager.GetUserId(User);

            var employeeRecord = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserID == userId);

            if (employeeRecord == null)
            {
                TempData["ErrorMessage"] = "Employee record not found.";
                return RedirectToAction("Index");
            }

            var myTimeZone = GetMalaysiaTimeZone();

            var claimsData = await _context.Claims
                .Where(c => c.EmployeeID == employeeRecord.EmployeeID)
                .Include(c => c.ClaimDocuments)
                .OrderByDescending(c => c.ClaimDate)
                .ToListAsync();

            var historyRecords = claimsData.Select(c =>
            {
                var document = c.ClaimDocuments.FirstOrDefault();

                var localClaimDate = TimeZoneInfo.ConvertTimeFromUtc(c.ClaimDate, myTimeZone);
                var localExpenseDate = TimeZoneInfo.ConvertTimeFromUtc(c.ExpensesDate, myTimeZone);

                return new EClaimHistoryRecord
                {
                    ID = c.ClaimID,
                    Type = c.Description,
                    Amount = c.Amount,
                    ClaimDate = localClaimDate,
                    ExpensesDate = localExpenseDate,
                    Status = c.Status,

                    ProofFileName = document?.FileName ?? "N/A",
                    ProofFilePath = document?.FilePath ?? "#"
                };
            }).ToList();

            var viewModel = new EClaimHistoryViewModel
            {
                HistoryRecords = historyRecords
            };

            return View(viewModel);
        }


        [HttpGet]
        public async Task<IActionResult> EmployeePayroll()
        {
            var userId = _userManager.GetUserId(User);
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserID == userId);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee record not found.";
                return RedirectToAction("Index");
            }

            var myTimeZone = GetMalaysiaTimeZone();
            var nowInMYT = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, myTimeZone);
            int currentMonth = nowInMYT.Month;
            int currentYear = nowInMYT.Year;

            var allPayrolls = await _context.Payrolls
                .Where(p => p.EmployeeID == employee.EmployeeID)
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ToListAsync();

            var viewModel = new PayrollViewModel
            {
                CurrentMonthDisplay = nowInMYT.ToString("MMMM yyyy")
            };

            var currentPayroll = allPayrolls
                .FirstOrDefault(p => p.Month == currentMonth && p.Year == currentYear);

            if (currentPayroll != null)
            {
                viewModel.CurrentMonthNetPay = currentPayroll.NetSalary;
                viewModel.CurrentMonthStatus = currentPayroll.PaymentDate.HasValue ? "Paid" : "Pending";
                viewModel.CurrentMonthPayrollID = currentPayroll.PayrollID;
                allPayrolls.Remove(currentPayroll);
            }
            else
            {
                viewModel.CurrentMonthNetPay = 0.00m;
                viewModel.CurrentMonthStatus = "Pending";
            }

            viewModel.PreviousPayslips = allPayrolls.Select(p => new PayslipRecord
            {
                ID = p.PayrollID,
                MonthYear = new DateTime(p.Year, p.Month, 1).ToString("MMMM yyyy"),
                NetPay = p.NetSalary,
                Status = p.PaymentDate.HasValue ? "Paid" : "Pending"
            }).ToList();

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeViewPayslip(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Authentication required.";
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserID == userId);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee record not found for this user.";
                return RedirectToAction("Index");
            }

            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Position)
                .FirstOrDefaultAsync(p => p.PayrollID == id && p.EmployeeID == employee.EmployeeID);

            if (payroll == null)
            {
                TempData["ErrorMessage"] = "Payslip record not found or access denied.";
                return RedirectToAction("EmployeePayroll");
            }

            var viewModel = new EmployeePayslipDetailViewModel
            {
                Month = payroll.Month,
                Year = payroll.Year,
                PaymentDate = payroll.PaymentDate,

                EmployeeName = $"{payroll.Employee.FirstName} {payroll.Employee.LastName}",
                EmployeeIC = payroll.Employee.ICNumber,
                EmployeeDepartment = payroll.Employee.Department.DepartmentName,
                EmployeePosition = payroll.Employee.Position.PositionTitle,

                BasicSalary = payroll.Employee.BasicSalary,
                Allowance = payroll.Allowances,
                EPFEmployee = payroll.EPFEmployee,
                EPFEmployer = payroll.EPFEmployer,
                SOCSOEmployee = payroll.SOCSOEmployee,
                SOCSOEmployer = payroll.SOCSOEmployer,
                EISEmployee = payroll.EISEmployee,
                EISEmployer = payroll.EISEmployer,
                PCB = payroll.PCB,
                OtherDeductions = payroll.Deductions,
                OtherDeductionDescription = "Other",
                NetSalary = payroll.NetSalary,

                Status = payroll.PaymentDate.HasValue ? "Paid" : "Pending"
            };

            return View(viewModel);
        }


        private DateTime? GetCombinedDateTime(DateTime date, TimeSpan? timeSpan)
        {
            if (timeSpan.HasValue)
            {
                return date.Date.Add(timeSpan.Value);
            }
            return null;
        }

        // GET: /Employee/EmployeeAttendance
        public async Task<IActionResult> EmployeeAttendance()
        {
            var userId = _userManager.GetUserId(User);
            var employeeRecord = await _context.Employees.FirstOrDefaultAsync(e => e.UserID == userId);

            if (employeeRecord == null)
            {
                TempData["ErrorMessage"] = "Employee record not found.";
                return RedirectToAction("Index", "Home");
            }

            var lastEntry = await _context.Attendance
                .Where(a => a.EmployeeID == employeeRecord.EmployeeID)
                .OrderByDescending(a => a.Date)
                .ThenByDescending(a => a.CheckInTime) 
                .FirstOrDefaultAsync();

            DateTime? lastClockIn = lastEntry != null ? GetCombinedDateTime(lastEntry.Date, lastEntry.CheckInTime) : null;
            DateTime? lastClockOut = lastEntry != null ? GetCombinedDateTime(lastEntry.Date, lastEntry.CheckOutTime) : null;

            bool isCurrentlyClockedIn = (lastEntry != null && lastEntry.CheckInTime.HasValue && !lastEntry.CheckOutTime.HasValue);

            var model = new AttendanceViewModel
            {
                LastClockIn = lastClockIn,
                LastClockOut = lastClockOut,
                IsClockedIn = isCurrentlyClockedIn
            };

            return View(model);
        }

        public IActionResult EmployeeFaceRecognition()
        {
            return View();
        }

        private int GetLoggedInEmployeeId()
        {
            var userId = _userManager.GetUserId(User);
            return _context.Employees
                .Where(e => e.UserID == userId)
                .Select(e => e.EmployeeID)
                .FirstOrDefault();
        }


        public class FaceVerificationRequestBody
        {
            public string ImageData { get; set; } = "";
            public string ActionType { get; set; } = "ClockIn"; 
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] 
        public async Task<IActionResult> VerifyFaceAndProcess([FromBody] FaceVerificationRequestBody req)
        {
            try
            {
                if (req == null || string.IsNullOrEmpty(req.ImageData))
                    return Json(new { success = false, message = "Invalid image data." });

                var employeeId = GetLoggedInEmployeeId();
                if (employeeId == 0)
                    return Json(new { success = false, message = "Invalid employee session." });

                var employee = await _context.Employees.FindAsync(employeeId);
                if (employee == null)
                    return Json(new { success = false, message = "Employee not found." });

                var referenceUrl = employee.ProfilePicturePath; 
                if (string.IsNullOrEmpty(referenceUrl))
                    return Json(new { success = false, message = "No stored profile photo found. Contact HR." });

                var (isVerified, message) = await _faceRecognitionService.VerifyFaceAsync(employeeId, req.ImageData, referenceUrl);

                if (!isVerified)
                    return Json(new { success = false, message = "Face not recognized. " + message });

                if (string.Equals(req.ActionType, "ClockOut", StringComparison.OrdinalIgnoreCase))
                {
                    var attendance = await ClockOutEmployee(employeeId);
                    string lastIn = attendance?.Date != null && attendance.CheckInTime.HasValue
                        ? (attendance.Date + attendance.CheckInTime.Value).ToLocalTime().ToString("HH:mm dd/MM/yyyy")
                        : "N/A";

                    string lastOut = attendance?.Date != null && attendance.CheckOutTime.HasValue
                        ? (attendance.Date + attendance.CheckOutTime.Value).ToLocalTime().ToString("HH:mm dd/MM/yyyy")
                        : "N/A";

                    return Json(new
                    {
                        success = true,
                        message = "Face verified. Clock-out recorded.",
                        isClockedIn = false,
                        lastClockIn = lastIn,
                        lastClockOut = lastOut
                    });
                }
                else 
                {
                    var attendance = await ClockInEmployee(employeeId);
                    string lastIn = attendance?.Date != null && attendance.CheckInTime.HasValue
                        ? (attendance.Date + attendance.CheckInTime.Value).ToLocalTime().ToString("HH:mm dd/MM/yyyy")
                        : "N/A";

                    return Json(new
                    {
                        success = true,
                        message = "Face verified. Clock-in recorded.",
                        isClockedIn = true,
                        lastClockIn = lastIn,
                        lastClockOut = "N/A"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VerifyFaceAndProcess failed for EmployeeId.");
                return Json(new { success = false, message = "System error: " + ex.Message });
            }
        }



        private async Task<Attendance> ClockInEmployee(int employeeId)
        {
            var today = DateTime.UtcNow.Date;

            var existingAttendance = await _context.Attendance
                .FirstOrDefaultAsync(a => a.EmployeeID == employeeId &&
                                          a.Date.Date == today);

            if (existingAttendance != null)
            {
                return existingAttendance; 
            }

            var newAttendance = new Attendance
            {
                EmployeeID = employeeId,
                Date = today,
                CheckInTime = DateTime.UtcNow.TimeOfDay
            };

            _context.Attendance.Add(newAttendance);
            await _context.SaveChangesAsync();

            return newAttendance;
        }

        private async Task<Attendance?> ClockOutEmployee(int employeeId)
        {
            var today = DateTime.UtcNow.Date;

            var existing = await _context.Attendance
                .Where(a => a.EmployeeID == employeeId && a.Date == today)
                .OrderByDescending(a => a.AttendanceID)
                .FirstOrDefaultAsync();

            if (existing == null)
            {
                return null;
            }

            if (existing.CheckOutTime.HasValue)
            {
                return existing;
            }

            existing.CheckOutTime = DateTime.UtcNow.TimeOfDay;

            _context.Attendance.Update(existing);
            await _context.SaveChangesAsync();

            return existing;
        }



    }
}