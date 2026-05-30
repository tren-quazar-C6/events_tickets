using events_tickets.Models;
using events_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers;

public class AuthController : Controller
{
    private readonly ApiService _api;
    private readonly SessionService _session;

    public AuthController(ApiService api, SessionService session)
    {
        _api = api;
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
        var result = await _api.LoginAsync(model.Email, model.Password);
        if (!result.Success)
        {
            TempData["message"] = result.Message ?? "Login failed";
            TempData["success"] = "False";
            return View(model);
        }

        var data = result.Data!;
        _session.SetToken(data.Token!);
        _session.SetEmployee(new Employee
        {
            IdStaff = data.IdStaff,
            Nombre = data.Nombre ?? "",
            Email = data.Email ?? "",
            Rol = data.Rol ?? ""
        });

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        _session.Clear();
        return RedirectToAction("Login");
    }
}