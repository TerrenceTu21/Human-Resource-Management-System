using fyphrms.Data; 
using fyphrms.Models; 
using fyphrms.Models.HR;
using fyphrms.Services;
using fyphrms.Services.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; 
using Microsoft.Extensions.Logging; 
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace fyphrms.Controllers
{
    [Authorize(Roles = "HR Manager")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HRController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PayrollCalculatorService _payrollCalculatorService;
        private readonly HttpClient _httpClient;
        private readonly SupabaseConfig _supabaseConfig;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        private const string SupabaseBucketName = "claim-document";
        private const string SupabaseUrlBase = "https://jgkkkdjcxqwcxjjilqfe.supabase.co/storage/v1";

        // Constructor injects DbContext and ILogger
        public HRController(ApplicationDbContext context, ILogger<HRController> logger, UserManager<ApplicationUser> userManager, PayrollCalculatorService payrollCalculatorService, HttpClient httpClient,
            SupabaseConfig supabaseConfig, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _payrollCalculatorService = payrollCalculatorService;
            _httpClient = httpClient;
            _supabaseConfig = supabaseConfig;
            _configuration = configuration;
            _emailService = emailService;
        }

        // --- GET: /HR/Index (Dashboard) ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 1. Fetch all required counts concurrently for performance
            var totalEmployees = await _context.Employees.AsNoTracking().Where(e => e.IsActive).CountAsync();
            var pendingLeaves = await _context.Leaves.AsNoTracking().CountAsync(l => l.Status == "Pending");
            var pendingClaims = await _context.Claims.AsNoTracking().CountAsync(c => c.Status == "Pending");

            // 2. Fetch the logged-in user's name
            var user = await _userManager.GetUserAsync(User);
            var userName = user?.UserName ?? "HR Manager";

            var today = DateOnly.FromDateTime(DateTime.Now);
            var upcomingHolidays = await _context.Holidays
                .Where(h => h.Date >= today)
                .OrderBy(h => h.Date)
                .Take(3)
                .ToListAsync();

            // 3. Create the ViewModel
            var model = new HRDashboardViewModel
            {
                UserName = userName,
                TotalEmployees = totalEmployees,
                PendingLeaves = pendingLeaves,
                PendingClaims = pendingClaims,
                UpcomingHolidays = upcomingHolidays
            };

            return View(model);
        }

        // --- GET: /HR/HREmployee (Employee List using EF Core) ---
        public async Task<IActionResult> HREmployee(string sortBy, string sortOrder, string searchString)
        {
            var employeeListModel = new HREmployeeListViewModel { Employees = new List<HREmployeeViewModel>() };

            // Set default and current sort or search state
            sortBy = string.IsNullOrEmpty(sortBy) ? "ID" : sortBy;
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "asc" : sortOrder;

            // Pass the current state to the View via ViewBag
            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentOrder = sortOrder;
            ViewBag.CurrentFilter = searchString; 

            try
            {
                // Fetch base query including related Position and Department data
                IQueryable<Employee> employeesQuery = _context.Employees
                    .Include(e => e.Position)
                    .Include(e => e.Department)
                    .Where(e => e.IsActive);

                // Apply search filter (before sorting)
                if (!string.IsNullOrEmpty(searchString))
                {
                    // Convert search term to lowercase for case-insensitive search
                    var search = searchString.ToLower();

                    employeesQuery = employeesQuery.Where(e =>
                        // Search by Employee Name (FirstName or LastName)
                        e.FirstName.ToLower().Contains(search) ||
                        e.LastName.ToLower().Contains(search) ||
                        // Search by Position Title 
                        (e.Position != null && e.Position.PositionTitle.ToLower().Contains(search)) ||
                        // Search by Department Name 
                        (e.Department != null && e.Department.DepartmentName.ToLower().Contains(search)) ||
                        // Search by Email
                        e.Email.ToLower().Contains(search) ||
                        // Search by Employee ID 
                        e.EmployeeID.ToString().Contains(search));
                }

                switch (sortBy)
                {
                    case "Name":
                        if (sortOrder == "desc")
                            employeesQuery = employeesQuery.OrderByDescending(e => e.FirstName).ThenByDescending(e => e.LastName);
                        else
                            employeesQuery = employeesQuery.OrderBy(e => e.FirstName).ThenBy(e => e.LastName);
                        break;

                    case "Position":
                        if (sortOrder == "desc")
                            employeesQuery = employeesQuery.OrderByDescending(e => e.Position.PositionTitle);
                        else
                            employeesQuery = employeesQuery.OrderBy(e => e.Position.PositionTitle);
                        break;

                    case "Department":
                        if (sortOrder == "desc")
                            employeesQuery = employeesQuery.OrderByDescending(e => e.Department.DepartmentName);
                        else
                            employeesQuery = employeesQuery.OrderBy(e => e.Department.DepartmentName);
                        break;

                    case "Email":
                        if (sortOrder == "desc")
                            employeesQuery = employeesQuery.OrderByDescending(e => e.Email);
                        else
                            employeesQuery = employeesQuery.OrderBy(e => e.Email);
                        break;

                    case "DateJoined":
                        if (sortOrder == "desc")
                            employeesQuery = employeesQuery.OrderByDescending(e => e.JoinDate);
                        else
                            employeesQuery = employeesQuery.OrderBy(e => e.JoinDate);
                        break;

                    case "ID": // Default sort
                    default:
                        if (sortOrder == "desc")
                            employeesQuery = employeesQuery.OrderByDescending(e => e.EmployeeID);
                        else
                            employeesQuery = employeesQuery.OrderBy(e => e.EmployeeID);
                        break;
                }

                var employeesData = await employeesQuery
                    .Select(e => new HREmployeeViewModel
                    {
                        ID = e.EmployeeID,
                        Name = e.FirstName + " " + e.LastName,
                        Email = e.Email,
                        DateJoined = e.JoinDate,
                        Position = e.Position.PositionTitle,
                        Department = e.Department.DepartmentName
                    })
                    .ToListAsync();

                employeeListModel.Employees = employeesData;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load employee data due to a database access error.";
            }

            return View(employeeListModel);
        }

        // GET: /HR/HREmployeeDetails/{id} ---
        [HttpGet]
        public async Task<IActionResult> HREmployeeDetails(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.EmployeeID == id);

            if (employee == null)
            {
                TempData["ErrorMessage"] = $"Employee with ID {id} not found.";
                return RedirectToAction("HREmployee");
            }

            var viewModel = new EmployeeDetailsViewModel
            {
                EmployeeID = employee.EmployeeID,
                UserID = employee.UserID,
                FullName = employee.FirstName + " " + employee.LastName,
                ICNumber = employee.ICNumber,
                ProfilePicturePath = employee.ProfilePicturePath,
                Email = employee.Email,
                ContactNumber = employee.ContactNumber,
                DateOfBirth = employee.DateOfBirth,
                Gender = employee.Gender,
                Address = employee.Address,
                BasicSalary = employee.BasicSalary,
                JoinDate = employee.JoinDate,
                EmploymentType = employee.EmploymentType,
                DepartmentName = employee.Department.DepartmentName,
                PositionTitle = employee.Position.PositionTitle
            };

            return View(viewModel);
        }

        // GET: /HR/AddEmployee
        [HttpGet]
        public async Task<IActionResult> HRAddEmployee()
        {
            var viewModel = new EmployeeUserCreationViewModel
            {
                Departments = await _context.Departments.ToListAsync(),
                Positions = await _context.Positions.ToListAsync(),

                // Populate available roles for the dropdown
                AvailableRoles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Employee", Text = "Employee", Selected = true },
                    new SelectListItem { Value = "HR Manager", Text = "HR" }, 
                }
            };
            return View(viewModel);
        }

        // POST: /HR/AddEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HRAddEmployee(EmployeeUserCreationViewModel model)
        {
            // Repopulate dropdowns in case model state is invalid
            if (!ModelState.IsValid)
            {
                model.Departments = await _context.Departments.ToListAsync();
                model.Positions = await _context.Positions.ToListAsync();
                model.AvailableRoles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Employee", Text = "Employee", Selected = model.RoleName == "Employee" },
                    new SelectListItem { Value = "HR Manager", Text = "HR", Selected = model.RoleName == "HR" },
                };
                return View(model);
            }

            // --- 1. Create ApplicationUser (SystemUser) ---
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
            var createUserResult = await _userManager.CreateAsync(user, model.Password);

            if (!createUserResult.Succeeded)
            {
                foreach (var error in createUserResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, $"System User Creation Error: {error.Description}");
                }
                // Repopulate dropdowns and return view with errors
                model.Departments = await _context.Departments.ToListAsync();
                model.Positions = await _context.Positions.ToListAsync();
                model.AvailableRoles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Employee", Text = "Employee", Selected = model.RoleName == "Employee" },
                    new SelectListItem { Value = "HR Manager", Text = "HR", Selected = model.RoleName == "HR" },
                };
                return View(model);
            }

            // --- 2. Assign Role to ApplicationUser ---
            try
            {
                await _userManager.AddToRoleAsync(user, model.RoleName);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                var innerExceptionMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                _logger.LogError(dbEx, "Failed to assign role {RoleName} to user {Email}. Deleting created user.", model.RoleName, model.Email);
                // If role assignment fails, delete the user to prevent orphaned accounts
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, $"Error assigning role '{model.RoleName}': {dbEx.Message}. User account was removed.");
                // Repopulate dropdowns and return view with errors
                TempData["ErrorMessage"] = $"Database Error: Failed to save employee record. Please check related fields (Department/Position). Details: {innerExceptionMessage}";
                model.Departments = await _context.Departments.ToListAsync();
                model.Positions = await _context.Positions.ToListAsync();
                model.AvailableRoles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Employee", Text = "Employee", Selected = model.RoleName == "Employee" },
                    new SelectListItem { Value = "HR", Text = "HR", Selected = model.RoleName == "HR" }
                };
                return View(model);
            }

            string profilePicturePath = null;
            if (model.ProfilePictureFile != null)
            {
                // Upload the file using the new dedicated helper
                profilePicturePath = await UploadProfilePictureToSupabase(model.ProfilePictureFile, user.Id);

                if (string.IsNullOrEmpty(profilePicturePath))
                {
                    _logger.LogError("Profile picture upload failed for new user {UserId}. Deleting user account to rollback.", user.Id);
                    await _userManager.DeleteAsync(user); 

                    ModelState.AddModelError(string.Empty, "Error: Failed to upload profile picture. Employee creation cancelled.");

                    model.Departments = await _context.Departments.ToListAsync();
                    model.Positions = await _context.Positions.ToListAsync();
                    model.AvailableRoles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Employee", Text = "Employee", Selected = model.RoleName == "Employee" },
                new SelectListItem { Value = "HR Manager", Text = "HR", Selected = model.RoleName == "HR" },
            };
                    return View(model);
                }
            }


            // --- 3. Create Employee HR Record ---
            var employee = new Employee
            {
                UserID = user.Id, 
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email, 
                ContactNumber = model.ContactNumber,
                DateOfBirth = DateTime.SpecifyKind(model.DateOfBirth, DateTimeKind.Utc),
                Gender = model.Gender,
                Address = model.Address,
                BasicSalary = model.BasicSalary,
                JoinDate = DateTime.SpecifyKind(model.JoinDate, DateTimeKind.Utc),
                EmploymentType = model.EmploymentType,
                DepartmentID = model.DepartmentID,
                PositionID = model.PositionID,
                ICNumber = model.ICNumber,
                ProfilePicturePath = profilePicturePath
            };

            try
            {
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
                await SetInitialLeaveEntitlements(employee.EmployeeID);
                TempData["SuccessMessage"] = $"Employee '{model.FirstName} {model.LastName}' and System User created successfully with role '{model.RoleName}'.";
                return RedirectToAction("HREmployee"); 
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                var innerExceptionMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                _logger.LogError(dbEx, "Failed to save Employee HR record for user {Email}. Attempting to delete created user.", model.Email);
                // If HR record creation fails, attempt to delete the Identity user
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, $"Error creating employee HR record: {dbEx.Message}. System user was removed.");
                TempData["ErrorMessage"] = $"Database Error: Failed to save employee record. Please check related fields (Department/Position). Details: {innerExceptionMessage}";
 
                model.Departments = await _context.Departments.ToListAsync();
                model.Positions = await _context.Positions.ToListAsync();
                model.AvailableRoles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Employee", Text = "Employee", Selected = model.RoleName == "Employee" },
                    new SelectListItem { Value = "HR Manager", Text = "HR", Selected = model.RoleName == "HR" },
                };
                return View(model);
            }
        }

        private async Task<string> UploadProfilePictureToSupabase(IFormFile file, string userId)
        {
            var bucketName = "profile-picture";

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
            {
                _logger.LogWarning("Profile picture upload failed for user {UserId}: Invalid file type {Extension}", userId, extension);
                return null; 
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var pathInBucket = $"profiles/{userId}/{uniqueFileName}";

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
                    _logger.LogError("Supabase Profile Picture Upload failed. Status: {Status}. Response: {Error}", response.StatusCode, errorContent);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HttpClient Profile Picture Upload failed for user {UserId}", userId);
                return null;
            }
        }

        private async Task SetInitialLeaveEntitlements(int employeeId)
        {
            int currentYear = DateTime.Now.Year;

            // Define default entitlement days for a full-year employee
            var defaultEntitlements = new Dictionary<string, decimal>
            {
                { "Annual Leave", 14 },
                { "Sick Leave", 14 },
                { "Unpaid Leave", 0 }, 
                { "Emergency Leave", 3 },
                { "Hospitalization Leave", 60 }
            };

            var requiredLeaveTypes = await _context.LeaveTypes
                .Where(lt => defaultEntitlements.Keys.Contains(lt.TypeName))
                .ToListAsync();

            var entitlementsToAdd = new List<Models.LeaveEntitlement>();
            var joinDate = _context.Employees
                                .Where(e => e.EmployeeID == employeeId)
                                .Select(e => e.JoinDate)
                                .FirstOrDefault();

            foreach (var leaveType in requiredLeaveTypes)
            {
                if (defaultEntitlements.TryGetValue(leaveType.TypeName, out decimal totalDays))
                {
                    if (leaveType.TypeName.Equals("Annual Leave", StringComparison.OrdinalIgnoreCase))
                    {
                        if (joinDate.Year == currentYear)
                        {
                            int monthsRemaining = 12 - (joinDate.Month - 1);
                            totalDays = Math.Round(totalDays * monthsRemaining / 12, 0); 
                        }
                    }

                    entitlementsToAdd.Add(new Models.LeaveEntitlement
                    {
                        EmployeeID = employeeId,
                        LeaveTypeID = leaveType.LeaveTypeID,
                        Year = currentYear,
                        TotalDays = (int)totalDays
                    });
                }
            }

            if (entitlementsToAdd.Any())
            {
                _context.LeaveEntitlements.AddRange(entitlementsToAdd);

                await _context.SaveChangesAsync();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPositionsByDepartment(int departmentId)
        {
            if (departmentId <= 0)
            {
                // Return an empty array if the ID is invalid
                return Json(new List<Position>());
            }

            try
            {
                // Fetch only positions where the DepartmentID matches the selected ID
                var positions = await _context.Positions
                    .Where(p => p.DepartmentID == departmentId)
                    .Select(p => new
                    {
                        PositionID = p.PositionID,
                        PositionTitle = p.PositionTitle
                    })
                    .ToListAsync();

                // Return the data as JSON
                return Json(positions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve positions for department ID {DepartmentId}.", departmentId);
                // Return an empty list on failure
                return StatusCode(500, new List<Position>());
            }
        }


        // --- GET: /HR/HRLeaveManagement?filter=... ---
        [HttpGet]
        public async Task<IActionResult> HRLeaveManagement(string filter = "Pending")
        {
            // 1. Determine the filter criteria (Default to Pending)
            string normalizedFilter = filter?.ToLower() ?? "pending";

            IQueryable<Leave> leavesQuery = _context.Leaves
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .AsNoTracking();

            // 2. Apply filtering based on status
            switch (normalizedFilter)
            {
                case "pending":
                    leavesQuery = leavesQuery.Where(l => l.Status == "Pending");
                    break;
                case "approved":
                    leavesQuery = leavesQuery.Where(l => l.Status == "Approved");
                    break;
                case "cancelled":
                    // Combine Cancelled and Rejected for the 'Cancelled' button
                    leavesQuery = leavesQuery.Where(l => l.Status == "Cancelled" || l.Status == "Rejected");
                    break;
                case "all":
                    // No additional filter applied
                    break;
                default:
                    leavesQuery = leavesQuery.Where(l => l.Status == "Pending");
                    normalizedFilter = "pending";
                    break;
            }

            // 3. Execute the query and map to ViewModel (Projection)
            var allRequests = await leavesQuery
                .OrderByDescending(l => l.StartDate)
                .Select(l => new LeaveRequestViewModel
                {
                    ID = l.LeaveID,
                    EmployeeID = l.EmployeeID,
                    EmployeeFullName = l.Employee.FirstName + " " + l.Employee.LastName,
                    LeaveType = l.LeaveType.TypeName,
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    // Calculate days: Add 1 to include both start and end dates
                    Days = (int)Math.Ceiling((l.EndDate - l.StartDate).TotalDays) + 1,
                    // Shorten the reason for the table view
                    ReasonSummary = l.Reason.Length > 30 ? l.Reason.Substring(0, 30) + "..." : l.Reason,
                    RejectReason = l.RejectReason,
                    Status = l.Status,
                    ProofPath = l.ProofPath
                })
                .ToListAsync();

            // 4. Create and return the final ViewModel
            var model = new HRLeaveManagementViewModel
            {
                LeaveRequests = allRequests,
                // Ensure the filter status returned is normalized
                ActiveFilter = normalizedFilter
            };

            return View(model);
        }

        private async Task<IActionResult> UpdateLeaveStatus(int leaveId, string newStatus, string rejectReason = null)
        {
            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .FirstOrDefaultAsync(l => l.LeaveID == leaveId);
            if (leave == null)
            {
                TempData["ErrorMessage"] = "Leave request not found.";
                return RedirectToAction("HRLeaveManagement");
            }

            if (leave.Status != "Pending")
            {
                TempData["ErrorMessage"] = $"Leave request ID {leaveId} is already {leave.Status}. Status can only be changed from Pending.";
                return RedirectToAction("HRLeaveManagement");
            }

            leave.Status = newStatus;

            if (newStatus == "Rejected" && !string.IsNullOrWhiteSpace(rejectReason))
            {
                leave.RejectReason = rejectReason;
            }

            var identityUser = await _userManager.GetUserAsync(User);

            var hrEmployee = await _context.Employees
                .AsNoTracking() 
                .Where(e => e.UserID == identityUser.Id)
                .Select(e => e.EmployeeID)
                .FirstOrDefaultAsync();

            if (hrEmployee == 0)
            {
                TempData["ErrorMessage"] = "Error: HR Manager's Employee record could not be found to assign approval.";
                return RedirectToAction("HRLeaveManagement");
            }

            leave.ApprovedBy = hrEmployee; 

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Leave request ID {leaveId} successfully {newStatus}.";

                var employeeEmail = leave.Employee.Email;
                if (!string.IsNullOrEmpty(employeeEmail))
                {
                    _ = _emailService.SendLeaveStatusEmailAsync(leave, employeeEmail).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update leave status for ID {LeaveId} to {Status}.", leaveId, newStatus);
                TempData["ErrorMessage"] = "A database error occurred while updating the status.";
            }

            return RedirectToAction("HRLeaveManagement", new { filter = leave.Status });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            return await UpdateLeaveStatus(id, "Approved");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectLeave(int id, string RejectReason)
        {
            return await UpdateLeaveStatus(id, "Rejected", RejectReason);
        }


        private TimeZoneInfo GetMalaysiaTimeZone()
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
        }

        public async Task<IActionResult> HRClaimManagement(string filter = "All")
        {
            var myTimeZone = GetMalaysiaTimeZone();

            var claimsQuery = _context.Claims
                .Include(c => c.Employee)
                .Include(c => c.ClaimDocuments)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter) && !filter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var lowerCaseFilter = filter.ToLower();

                claimsQuery = claimsQuery.Where(c => c.Status.ToLower() == lowerCaseFilter);
            }

            // 3. Execute Query
            var claimsData = await claimsQuery
                .OrderByDescending(c => c.ClaimDate)
                .ToListAsync();

            // 4. Map and Construct Proof URLs in Memory
            var hrClaimRecords = claimsData.Select(c =>
            {
                // Get the primary document details 
                var document = c.ClaimDocuments.FirstOrDefault();

                // Convert the stored UTC ClaimDate to Malaysia Local Time
                var localClaimDate = TimeZoneInfo.ConvertTimeFromUtc(c.ClaimDate, myTimeZone);

                // Build the URL using the file path stored in the database
                string proofUrl = null;
                if (document != null && !string.IsNullOrEmpty(document.FilePath))
                {
                    proofUrl = document.FilePath;
                }

                return new HRClaimViewModel
                {
                    ID = c.ClaimID,
                    EmployeeName = $"{c.Employee.FirstName} {c.Employee.LastName}",
                    ClaimDescription = c.Description,
                    Amount = c.Amount,
                    ClaimDate = localClaimDate, 
                    Status = c.Status,
                    RejectReason = c.RejectReason,
                    DateOfExpenses = c.ExpensesDate,
                    ClaimDocumentFileName = document?.FileName ?? "N/A",
                    ProofDownloadUrl = proofUrl 
                };
            }).ToList();

            var viewModel = new HRClaimManagementViewModel
            {
                Claims = hrClaimRecords,
                ActiveFilter = filter ?? "All"
            };

            return View(viewModel);
        }

        private async Task<IActionResult> ProcessClaimStatusUpdate(int claimId, string newStatus, string rejectReason = null)
        {
            var claim = await _context.Claims
                .Include(c => c.Employee)
                .FirstOrDefaultAsync(c => c.ClaimID == claimId);

            if (claim == null)
            {
                TempData["ErrorMessage"] = $"Error: Claim ID {claimId} not found.";
                return RedirectToAction("HRClaimManagement");
            }

            if (claim.Status != "Pending")
            {
                TempData["ErrorMessage"] = $"Claim ID {claimId} cannot be updated because its current status is '{claim.Status}'. Status can only be changed from Pending.";
                return RedirectToAction("HRClaimManagement");
            }

            claim.Status = newStatus;
            if (newStatus == "Rejected")
            {
                claim.RejectReason = rejectReason;
            }

            // --- Logic to find the ApprovedBy EmployeeID (HR Manager) ---
            var identityUser = await _userManager.GetUserAsync(User);

            if (identityUser == null)
            {
                TempData["ErrorMessage"] = "Authentication Error: User identity not found.";
                return RedirectToAction("HRClaimManagement");
            }

            // Find the EmployeeID based on the logged-in IdentityUser ID
            var hrEmployee = await _context.Employees
                .AsNoTracking()
                .Where(e => e.UserID == identityUser.Id)
                .Select(e => e.EmployeeID)
                .FirstOrDefaultAsync();

            if (hrEmployee == 0)
            {
                _logger.LogError("HR Manager's Employee record could not be found for status update (Identity ID: {UserId}).", identityUser.Id);
                TempData["ErrorMessage"] = "Error: HR Manager's Employee record could not be found to assign approval.";
                return RedirectToAction("HRClaimManagement");
            }

            claim.ApprovedBy = hrEmployee;

            try
            {
                // Save status and ApprovedBy changes
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Claim ID {claimId}  has been successfully {newStatus}.";

                var employeeEmail = claim.Employee?.Email;
                if (!string.IsNullOrEmpty(employeeEmail))
                {
                    _ = _emailService.SendClaimStatusEmailAsync(claim, employeeEmail).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update claim status for ID {ClaimId} to {Status}.", claimId, newStatus);
                TempData["ErrorMessage"] = $"A database error occurred while updating the status for Claim ID {claimId}.";
            }

            return RedirectToAction("HRClaimManagement");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(int id)
        {
            return await ProcessClaimStatusUpdate(id, "Approved");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(int id, string RejectReason)
        {
            if (string.IsNullOrWhiteSpace(RejectReason))
            {
                TempData["ErrorMessage"] = "Rejection failed: A rejection reason is required.";
                return RedirectToAction("HRClaimManagement");
            }
            return await ProcessClaimStatusUpdate(id, "Rejected", RejectReason);
        }

        
        // --- Action 1: HRPayrollManagement (Main Page - Grouped by Month) ---
        [HttpGet]
        public async Task<IActionResult> HRPayrollManagement()
        {
            var model = new HRPayrollViewModel();

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var employees = _context.Employees.ToList();

            model.MonthlySummaries = new List<MonthlyPayrollSummary>();

            var monthlySummary = new MonthlyPayrollSummary
            {
                Month = currentMonth,
                Year = currentYear,
                GroupID = "Group 1",
                Status = _context.Payrolls.Any(p => p.Month == currentMonth && p.Year == currentYear)
                         ? "Completed"
                         : "Pending"
            };

            if (monthlySummary.Status == "Completed")
            {
                monthlySummary.TotalNetPay = _context.Payrolls
                    .Where(p => p.Month == currentMonth && p.Year == currentYear)
                    .Sum(p => p.NetSalary);
            }
            else
            {
                decimal totalNetPay = 0;
                foreach (var emp in employees)
                {
                    var payslip = _payrollCalculatorService.CalculatePayslip(emp, currentMonth, currentYear);
                    totalNetPay += payslip.NetSalary;
                }
                monthlySummary.TotalNetPay = totalNetPay;
            }

            model.MonthlySummaries.Add(monthlySummary);

            return View(model);
        }

        // --- Action 2: HRViewPayslip (Intermediate Page - All Employees for a Month) ---
        [HttpGet]
        public async Task<IActionResult> HRViewPayslip(int month, int year)
        {
            string monthYearDisplay = new DateTime(year, month, 1).ToString("MMMM yyyy");

            var employees = await _context.Employees
                .OrderBy(e => e.FirstName)
                .ToListAsync();

            var existingPayrolls = await _context.Payrolls
                .Where(p => p.Month == month && p.Year == year)
                .ToDictionaryAsync(p => p.EmployeeID);

            var employeePayslipSummaries = new List<EmployeePayslipSummaryViewModel>();

            foreach (var employee in employees)
            {
                Payroll payroll;
                bool isSimulated = false;

                if (!existingPayrolls.TryGetValue(employee.EmployeeID, out payroll))
                {
                    payroll = _payrollCalculatorService.CalculatePayslip(employee, month, year);
                    isSimulated = true;
                }

                string status = isSimulated
                    ? "Calculated (Unsaved)"
                    : (payroll.PaymentDate.HasValue ? "Paid" : "Pending");

                employeePayslipSummaries.Add(new EmployeePayslipSummaryViewModel
                {
                    PayrollID = payroll.PayrollID,
                    EmployeeID = employee.EmployeeID,
                    Month = month,
                    Year = year,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    NetSalary = payroll.NetSalary,
                    Status = status,
                    IsSimulated = isSimulated,
                    BasicSalary = employee.BasicSalary,
                    EPFEmployee = payroll.EPFEmployee,
                    EPFEmployer = payroll.EPFEmployer,
                    SOCSOEmployee = payroll.SOCSOEmployee,
                    SOCSOEmployer = payroll.SOCSOEmployer,
                    EISEmployee = payroll.EISEmployee,
                    EISEmployer = payroll.EISEmployer,
                    PCB = payroll.PCB,
                    Deductions = payroll.Deductions,
                    Allowances = payroll.Allowances
                });
            }

            var viewModel = new HRViewPayslipViewModel
            {
                MonthYearDisplay = monthYearDisplay,
                EmployeePayslips = employeePayslipSummaries
            };

            return View(viewModel);
        }

        // --- Action 3: ViewPayslip (Final Detail Page for SAVED records) ---
        [HttpGet]
        public async Task<IActionResult> ViewPayslip(int id)
        {
            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.PayrollID == id);

            if (payroll == null)
            {
                TempData["ErrorMessage"] = $"Payslip ID {id} not found.";
                return RedirectToAction("HRPayrollManagement");
            }

            var viewModel = new ViewPayslipDetailViewModel
            {
                Payroll = payroll,
                Employee = payroll.Employee,
                IsPreview = false
            };

            return View(viewModel);
        }

        // --- Action 4: PreviewPayslip (Final Detail Page for SIMULATED records) ---
        [HttpGet]
        public async Task<IActionResult> PreviewPayslip(int employeeId, int month, int year)
        {
            var existingPayroll = await _context.Payrolls
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.EmployeeID == employeeId && p.Month == month && p.Year == year);

            if (existingPayroll != null)
            {
                TempData["InfoMessage"] = "The payslip was saved and is no longer a preview. Showing saved record.";
                return RedirectToAction("ViewPayslip", new { id = existingPayroll.PayrollID });
            }

            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToAction("HRPayrollManagement");
            }

            var simulatedPayroll = _payrollCalculatorService.CalculatePayslip(employee, month, year);

            simulatedPayroll.EmployeeID = employeeId;
            simulatedPayroll.Month = month;
            simulatedPayroll.Year = year;

            var viewModel = new ViewPayslipDetailViewModel
            {
                Payroll = simulatedPayroll,
                Employee = employee,
                IsPreview = true
            };

            return View("ViewPayslip", viewModel);
        }

        // --- Action 5: Process Payroll ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayroll(int month, int year)
        {
            DateTime paymentTimestamp = DateTime.UtcNow;

            var employees = await _context.Employees.ToListAsync();

            foreach (var emp in employees)
            {
                var existing = await _context.Payrolls
                    .FirstOrDefaultAsync(p => p.EmployeeID == emp.EmployeeID &&
                                              p.Month == month &&
                                              p.Year == year);

                if (existing != null)
                    continue;

                var calculation = _payrollCalculatorService.CalculatePayslip(emp, month, year);

                var payroll = new Payroll
                {
                    EmployeeID = emp.EmployeeID,
                    Month = month,
                    Year = year,
                    EPFEmployee = calculation.EPFEmployee,
                    EPFEmployer = calculation.EPFEmployer,
                    SOCSOEmployee = calculation.SOCSOEmployee,
                    SOCSOEmployer = calculation.SOCSOEmployer,
                    EISEmployee = calculation.EISEmployee,
                    EISEmployer = calculation.EISEmployer,
                    PCB = calculation.PCB,
                    Deductions = calculation.Deductions,
                    Allowances = calculation.Allowances,
                    NetSalary = calculation.NetSalary,
                    PaymentDate = paymentTimestamp
                };

                _context.Payrolls.Add(payroll);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payroll processed successfully (UTC timestamp saved).";
            return RedirectToAction("HRViewPayslip", new { month, year });
        }
        

        // GET: HR/HREditEmployee/
        [HttpGet]
        public async Task<IActionResult> HREditEmployee(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.EmployeeID == id);

            if (employee == null)
                return NotFound();

            var departments = await _context.Departments.ToListAsync();

            // Initially load positions only for employee's current department
            var positions = await _context.Positions
                .Where(p => p.DepartmentID == employee.DepartmentID)
                .ToListAsync();

            var model = new HREditEmployeeViewModel
            {
                EmployeeID = employee.EmployeeID,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                ContactNumber = employee.ContactNumber,
                BasicSalary = employee.BasicSalary,
                DepartmentID = employee.DepartmentID,
                PositionID = employee.PositionID,
                Address = employee.Address,
                EmploymentType = employee.EmploymentType,
                Departments = new SelectList(departments ?? new List<Department>(), "DepartmentID", "DepartmentName", employee.DepartmentID),
                Positions = new SelectList(positions ?? new List<Position>(), "PositionID", "PositionTitle", employee.PositionID)
            };

            return View(model);
        }
        
        // POST: HR/HREditEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HREditEmployee(HREditEmployeeViewModel model)
        {
            var employee = await _context.Employees.FindAsync(model.EmployeeID);
            if (employee == null)
                return NotFound();

            employee.FirstName = model.FirstName;
            employee.LastName = model.LastName;
            employee.Email = model.Email;
            employee.ContactNumber = model.ContactNumber;
            employee.BasicSalary = model.BasicSalary;
            employee.DepartmentID = model.DepartmentID;
            employee.PositionID = model.PositionID;
            employee.Address = model.Address;
            employee.EmploymentType = model.EmploymentType;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Employee updated successfully!";
                return RedirectToAction("HREmployee");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Failed to update employee. The email or contact number might already exist.");

                var departments = await _context.Departments.ToListAsync();
                var positions = await _context.Positions
                    .Where(p => p.DepartmentID == model.DepartmentID)
                    .ToListAsync();

                model.Departments = new SelectList(departments, "DepartmentID", "DepartmentName", model.DepartmentID);
                model.Positions = new SelectList(positions, "PositionID", "PositionTitle", model.PositionID);

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> HRDepartmentsPositions()
        {
            try
            {
                var departments = await _context.Departments
                    .Include(d => d.Positions) 
                    .ToListAsync();

                var vm = new DepartmentsPositionsPageViewModel();

                if (departments != null && departments.Count > 0)
                {
                    foreach (var d in departments)
                    {
                        var dp = new DepartmentPositionViewModel
                        {
                            DepartmentID = d.DepartmentID,
                            DepartmentName = d.DepartmentName ?? string.Empty,
                            Positions = d.Positions?
                                .Select(p => new PositionItem { PositionID = p.PositionID, PositionTitle = p.PositionTitle })
                                .ToList() ?? new List<PositionItem>()
                        };
                        vm.Departments.Add(dp);
                    }
                }
                else
                {
                    var depts = await _context.Departments.ToListAsync();
                    var positions = await _context.Positions.ToListAsync();

                    var group = positions.GroupBy(p => p.DepartmentID)
                                         .ToDictionary(g => g.Key, g => g.Select(p => new PositionItem { PositionID = p.PositionID, PositionTitle = p.PositionTitle }).ToList());

                    foreach (var d in depts)
                    {
                        group.TryGetValue(d.DepartmentID, out var posList);
                        vm.Departments.Add(new DepartmentPositionViewModel
                        {
                            DepartmentID = d.DepartmentID,
                            DepartmentName = d.DepartmentName ?? string.Empty,
                            Positions = posList ?? new List<PositionItem>()
                        });
                    }
                }

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Departments & Positions.");
                TempData["ErrorMessage"] = "Unable to load departments and positions at this time.";
                return RedirectToAction("HREmployee"); 
            }
        }

        [HttpPost]
        public async Task<IActionResult> HRAddPosition(int departmentId, string positionTitle)
        {
            if (string.IsNullOrWhiteSpace(positionTitle))
            {
                TempData["Error"] = "Position title cannot be empty.";
                return RedirectToAction("HRDepartmentsPositions");
            }

            var position = new Position
            {
                DepartmentID = departmentId,
                PositionTitle = positionTitle.Trim()
            };

            _context.Positions.Add(position);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Position added successfully!";
            return RedirectToAction("HRDepartmentsPositions");
        }

        // GET: HR/HRAddDepartment
        [HttpGet]
        public IActionResult HRAddDepartment()
        {
            var vm = new AddDepartmentViewModel();
            return View(vm);
        }

        // POST: HR/HRAddDepartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HRAddDepartment(AddDepartmentViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var name = vm.DepartmentName.Trim();
            var exists = await _context.Departments
                .AsNoTracking()
                .AnyAsync(d => d.DepartmentName.ToLower() == name.ToLower());

            if (exists)
            {
                ModelState.AddModelError(nameof(vm.DepartmentName), "A department with this name already exists.");
                return View(vm);
            }

            var maxId = await _context.Departments.MaxAsync(d => (int?)d.DepartmentID) ?? 0;

            var dept = new Department
            {
                DepartmentID = maxId + 1,
                DepartmentName = name
            };

            try
            {
                _context.Departments.Add(dept);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Department added successfully.";
                return RedirectToAction("HRDepartmentsPositions"); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add department");
                ModelState.AddModelError(string.Empty, "Unexpected error while saving. Please try again or contact admin.");
                return View(vm);
            }
        }

        public IActionResult Today() => RedirectToAction("HRAttendanceManagement", new { date = DateTime.UtcNow.Date });
        public IActionResult Yesterday() => RedirectToAction("HRAttendanceManagement", new { date = DateTime.UtcNow.Date.AddDays(-1) });
        public IActionResult ThisWeek() => RedirectToAction("HRAttendanceManagement", new { date = DateTime.UtcNow.Date.AddDays(-7) });
        public IActionResult ThisMonth() => RedirectToAction("HRAttendanceManagement", new { date = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1) });


        public async Task<IActionResult> HRAttendanceManagement(DateTime? date, string search)
        {
            if (!date.HasValue)
                date = DateTime.UtcNow;

            DateTime selectedDateUtc = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);

            var employees = await _context.Employees.ToListAsync();

            var attendanceRecords = await _context.Attendance
                .Where(a => a.Date.Date == selectedDateUtc.Date)
                .Include(a => a.Employee)
                .ToListAsync();

            var MYT = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var combinedList = new List<Attendance>();

            foreach (var emp in employees)
            {
                var record = attendanceRecords.FirstOrDefault(a => a.EmployeeID == emp.EmployeeID);

                if (record != null)
                {
                    // Convert stored UTC times → local for display
                    if (record.CheckInTime.HasValue)
                    {
                        DateTime utcCI = record.Date.Add(record.CheckInTime.Value);
                        var localCI = TimeZoneInfo.ConvertTimeFromUtc(utcCI, MYT).TimeOfDay;
                        record.CheckInTime = localCI;
                    }

                    if (record.CheckOutTime.HasValue)
                    {
                        DateTime utcCO = record.Date.Add(record.CheckOutTime.Value);
                        var localCO = TimeZoneInfo.ConvertTimeFromUtc(utcCO, MYT).TimeOfDay;
                        record.CheckOutTime = localCO;
                    }

                    combinedList.Add(record);
                }
                else
                {
                    combinedList.Add(new Attendance
                    {
                        Employee = emp,
                        EmployeeID = emp.EmployeeID,
                        Date = selectedDateUtc,
                        CheckInTime = null,
                        CheckOutTime = null
                    });
                }
            }

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                combinedList = combinedList
                    .Where(a => (a.Employee.FirstName + " " + a.Employee.LastName)
                    .ToLower().Contains(search.ToLower())).ToList();
            }

            // Summary
            int present = combinedList.Count(a => a.CheckInTime != null);
            int late = combinedList.Count(a => a.CheckInTime > new TimeSpan(9, 0, 0));
            int absent = combinedList.Count(a => a.CheckInTime == null);

            var vm = new HRAttendanceViewModel
            {
                SelectedDate = selectedDateUtc,
                AttendanceList = combinedList,
                AllEmployees = employees,
                SearchQuery = search ?? "",
                PresentCount = present,
                LateCount = late,
                AbsentCount = absent
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HRChangeEmployeePassword(int employeeId, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return RedirectToAction("HREmployeeDetails", new { id = employeeId });
            }

            // Get the employee
            var employee = await _context.Employees
                .Include(e => e.UserAccount) 
                .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);

            if (employee == null || employee.UserAccount == null)
            {
                TempData["Error"] = "Employee or user not found.";
                return RedirectToAction("HREmployee");
            }

            var user = employee.UserAccount;

            // Remove the current password if exists
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                TempData["Error"] = "Failed to remove existing password.";
                return RedirectToAction("HREmployeeDetails", new { id = employeeId });
            }

            // Add new password
            var addPasswordResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (addPasswordResult.Succeeded)
            {
                TempData["Success"] = "Password changed successfully.";
                return RedirectToAction("HREmployeeDetails", new { id = employeeId });
            }

            // Handle errors
            TempData["Error"] = string.Join("; ", addPasswordResult.Errors.Select(e => e.Description));
            return RedirectToAction("HREmployeeDetails", new { id = employeeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HRHideEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();

            employee.IsActive = false;
            _context.Update(employee);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Employee {employee.FirstName} {employee.LastName} has been hidden.";
            return RedirectToAction("HREmployee");
        }

        [HttpGet]
        public async Task<IActionResult> HRHolidays()
        {
            var holidays = await _context.Holidays.OrderBy(h => h.Date).ToListAsync();
            return View(holidays);
        }

        // GET: /HR/EditHoliday/5
        [HttpGet]
        public async Task<IActionResult> HREditHoliday(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();
            return View(holiday);
        }

        // POST: /HR/EditHoliday/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HREditHoliday(int id, Holiday model)
        {
            if (id != model.HolidayID) return BadRequest();

            if (!ModelState.IsValid) return View(model);

            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();

            holiday.Name = model.Name;
            holiday.Date = model.Date;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Holiday updated successfully!";
            return RedirectToAction("HRHolidays");
        }

        // POST: /HR/DeleteHoliday/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHoliday(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();

            _context.Holidays.Remove(holiday);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Holiday deleted successfully!";
            return RedirectToAction("HRHolidays");
        }

        // POST: /HR/AddHoliday
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHoliday(string Name, DateTime Date)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                TempData["ErrorMessage"] = "Holiday name cannot be empty.";
                return RedirectToAction("HRHolidays");
            }
            var holidayDate = DateOnly.FromDateTime(Date);
            var holiday = new Holiday
            {
                Name = Name,
                Date = holidayDate
            };

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Holiday added successfully!";
            return RedirectToAction("HRHolidays");
        }

        public async Task<IActionResult> HRReportManagement(string report = "Attendance", int? month = null, int? year = null)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var viewModel = new HRReportViewModel
            {
                SelectedReport = report,
                SelectedMonth = selectedMonth,
                SelectedYear = selectedYear
            };

            // --- Attendance Analytics ---
            if (report == "Attendance")
            {
                viewModel.AttendanceData = await _context.Attendance
                    .Where(a => a.Date.Month == selectedMonth && a.Date.Year == selectedYear)
                    .GroupBy(a => a.Date.Day)
                    .Select(g => new AttendanceAnalyticsModel
                    {
                        Month = g.Key.ToString(),
                        PresentDays = g.Count(a => a.CheckInTime.HasValue),
                        AbsentDays = g.Count(a => !a.CheckInTime.HasValue)
                    })
                    .ToListAsync();
            }


            // --- Leave Analytics ---
            if (report == "Leave")
            {
                var leaves = await _context.Leaves
                    .Include(l => l.LeaveType)
                    .Where(l => l.Status == "Approved"
                        && l.StartDate.Month == selectedMonth
                        && l.StartDate.Year == selectedYear)
                    .ToListAsync(); 

                viewModel.LeaveData = leaves
                    .GroupBy(l => l.LeaveType.TypeName)
                    .Select(g => new LeaveAnalyticsModel
                    {
                        LeaveType = g.Key,
                        TotalTaken = g.Sum(l => (l.EndDate - l.StartDate).Days + 1) 
                    })
                    .ToList();
            }


            // --- Claim Analytics (by Status) ---
            if (report == "Claim")
            {
                viewModel.EClaimData = await _context.Claims
                    .Where(c => c.ExpensesDate.Month == selectedMonth &&
                                c.ExpensesDate.Year == selectedYear)
                    .GroupBy(c => c.Status)
                    .Select(g => new EClaimAnalyticsModel
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(c => c.Amount)
                    })
                    .ToListAsync();
            }

            return View(viewModel);
        }

    }
}
