using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.IsInRole("Admin"))
            return RedirectToAction("Index", "Admin");
        if (User.IsInRole("HR Manager"))
            return RedirectToAction("Index", "HR");
        if (User.IsInRole("Employee"))
            return RedirectToAction("Index", "Employee");

        return View();
    }

    [AllowAnonymous] 
    public IActionResult Privacy()
    {
        return View();
    }

    
}