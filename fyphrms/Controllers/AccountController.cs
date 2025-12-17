using fyphrms.Data;
using fyphrms.Models;
using fyphrms.Models.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace fyphrms.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ApplicationDbContext _context;

        public AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users, ApplicationDbContext context)
        {
            _signIn = signIn;
            _users = users;
            _context = context;
        }

        //GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        //POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            foreach (var kv in Request.Form) Console.WriteLine($"{kv.Key} = {kv.Value}");

            if (!ModelState.IsValid) return View(model);

            var user = await _users.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserID == user.Id);

            if (employee == null || !employee.IsActive)
            {
                ModelState.AddModelError("", "Your account is deactivated. Please contact HR.");
                return View(model);
            }

            var res = await _signIn.PasswordSignInAsync(user.UserName, model.Password, false, lockoutOnFailure: false);

            if (!res.Succeeded)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (await _users.IsInRoleAsync(user, "Admin"))
                return RedirectToAction("Index", "Admin");
            if (await _users.IsInRoleAsync(user, "HR Manager"))
                return RedirectToAction("Index", "HR");
            if (await _users.IsInRoleAsync(user, "Employee"))
                return RedirectToAction("Index", "Employee");

            return RedirectToAction("Index", "Home");
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        //GET: /Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // 1. Get the current user's UserID (from Claims/Identity)
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Fetch the Employee, including Department and Position
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserID == currentUserId);

            if (employee == null)
            {
                return NotFound(); // Or redirect to an onboarding/error page
            }

            // 3. Map Employee to ViewModel
            var viewModel = new MyProfileViewModel
            {
                EmployeeID = employee.EmployeeID,
                UserID = employee.UserID,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                DateOfBirth = employee.DateOfBirth,
                Gender = employee.Gender,
                ContactNumber = employee.ContactNumber,
                Email = employee.Email,
                Address = employee.Address,
                ICNumber = employee.ICNumber,
                JoinDate = employee.JoinDate,
                EmploymentType = employee.EmploymentType,
                BasicSalary = employee.BasicSalary,
                ProfilePicturePath = employee.ProfilePicturePath,
                DepartmentName = employee.Department.DepartmentName, 
                PositionName = employee.Position.PositionTitle 
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateContactDetails([FromBody] ContactUpdateDto dto)
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "Invalid data provided." });

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeID == dto.EmployeeID);

            if (employee == null)
                return NotFound(new { success = false, message = "Employee not found." });

            employee.ContactNumber = dto.ContactNumber;
            employee.Address = dto.Address;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Contact details updated successfully." });
        }

        // GET: Display Change Password Page
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _users.GetUserAsync(User); // Current logged-in user
            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new ChangePasswordViewModel();
            return View(model);
        }


        // POST: Handle Change Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _users.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            // Verify current password
            var isCurrentPasswordValid = await _users.CheckPasswordAsync(user, model.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }

            // Change password
            var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Profile");
            }

            // Add errors from Identity
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }


    }
}
