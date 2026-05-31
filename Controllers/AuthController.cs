using events_tickets.Models;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers;

public class AuthController : Controller
{
    private readonly IEmployeeService _employees;
    private readonly SessionService _session;

    public AuthController(IEmployeeService employees, SessionService session)
    {
        _employees = employees;
        _session = session;
    }

    public IActionResult Login()
    {
        if (_session.IsAuthenticated())
            return RedirectToAction("Index", "Dashboard");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var employee = await _employees.LoginAsync(model.Email, model.Password);
        if (employee == null)
        {
            TempData["message"] = "Invalid credentials or employee is not authorized for ticket sales.";
            TempData["success"] = "False";
            return View(model);
        }

        _session.SetToken(Guid.NewGuid().ToString("N"));
        _session.SetEmployee(employee);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        _session.Clear();
        return RedirectToAction("Login");
    }
}
