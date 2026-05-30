using Microsoft.AspNetCore.Mvc;

namespace events_tickets.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Login", "Auth");
}