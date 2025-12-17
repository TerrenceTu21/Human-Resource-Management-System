using fyphrms.Data;
using fyphrms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fyphrms.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            ILogger<AdminController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        // GET: /AdminDashboard/Index
        public async Task<IActionResult> Index()
        {
            int totalEmployees = await _context.Employees.CountAsync();
            int totalDepartments = await _context.Departments.CountAsync();

            int newUsers = 0;

            var employeeData = await _context.Employees
                .Select(e => new EmployeeListItem
                {
                    EmployeeID = e.EmployeeID,
                    FullName = e.FirstName + " " + e.LastName,
                    Email = e.Email,
                    Position = e.Position.PositionTitle,
                    Department = e.Department.DepartmentName
                })
                .ToListAsync();

            var allUsers = await _userManager.Users.ToListAsync();
            var systemUsersList = new List<UserListItem>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);

                systemUsersList.Add(new UserListItem
                {
                    UserID = user.Id,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    CurrentRole = roles.FirstOrDefault() ?? "No Role Assigned"
                });
            }

            var viewModel = new AdminDashboardViewModel
            {
                TotalEmployees = totalEmployees,
                TotalDepartments = totalDepartments,
                Employees = employeeData,
                SystemUsers = systemUsersList 
            };

            return View("~/Views/Admin/Index.cshtml", viewModel);
        }

        // GET: Admin/CreateEmployee
        [HttpGet]
        public async Task<IActionResult> CreateEmployee()
        {
            var viewModel = new EmployeeUserCreationViewModel
            {
                Departments = await _context.Departments.ToListAsync(),
                Positions = await _context.Positions.ToListAsync()
            };
            return View(viewModel);
        }

        // POST: Admin/CreateEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(EmployeeUserCreationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Departments = await _context.Departments.ToListAsync();
                model.Positions = await _context.Positions.ToListAsync();
                return View(model);
            }

            var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, $"User creation error: {error.Description}");
                }
                model.Departments = await _context.Departments.ToListAsync();
                model.Positions = await _context.Positions.ToListAsync();
                return View(model);
            }

            var employee = new Employee
            {
                UserID = user.Id, 
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                ContactNumber = model.ContactNumber,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                BasicSalary = model.BasicSalary,
                JoinDate = model.JoinDate,
                EmploymentType = model.EmploymentType,
                DepartmentID = model.DepartmentID,
                PositionID = model.PositionID
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            await _userManager.AddToRoleAsync(user, "Employee");

            TempData["SuccessMessage"] = $"Employee {model.FirstName} created successfully and assigned 'Employee' role.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSystemUser(SimpleUserCreationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Validation failed. Please ensure all fields are correct.";
                return RedirectToAction("Index");
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                try
                {
                    _logger.LogInformation($"Successfully created Identity user with ID: {user.Id} and Email: {user.Email}");
                    await _userManager.AddToRoleAsync(user, model.RoleName);

                    TempData["SuccessMessage"] = $"System User **{model.Email}** created and assigned role **{model.RoleName}**.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    await _userManager.DeleteAsync(user);
                    _logger.LogError(ex, "Role assignment failed for user {Email}.", model.Email);

                    TempData["ErrorMessage"] = $"User creation failed during role assignment. User was deleted. Error: {ex.Message}";
                    return RedirectToAction("Index");
                }
            }

            var identityErrors = string.Join(", ", result.Errors.Select(e => e.Description));
            TempData["ErrorMessage"] = $"Failed to create user: {identityErrors}";
            return RedirectToAction("Index");
        }
    }
}
