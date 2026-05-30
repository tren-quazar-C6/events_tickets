using System.Text.Json;
using events_tickets.Models;

namespace events_tickets.Services;

public class SessionService
{
    private readonly IHttpContextAccessor _accessor;

    public SessionService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    public string? GetToken() => Session.GetString("jwt_token");

    public void SetToken(string token) => Session.SetString("jwt_token", token);

    public void SetEmployee(Employee employee)
        => Session.SetString("employee", JsonSerializer.Serialize(employee));

    public Employee? GetEmployee()
    {
        var json = Session.GetString("employee");
        if (json == null) return null;
        return JsonSerializer.Deserialize<Employee>(json);
    }

    public bool IsAuthenticated() => GetToken() != null;

    // The API returns a role string (not individual permissions).
    // When the API adds a /employees/me endpoint with permissions,
    // replace this method body with a real permission check.
    public bool HasPermission(string permission) => IsAuthenticated();

    public void Clear() => Session.Clear();
}