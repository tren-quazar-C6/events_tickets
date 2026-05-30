using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using events_tickets.Models;
using events_tickets.Responses;

namespace events_tickets.Services;

public class ApiService
{
    private readonly IHttpClientFactory _factory;
    private readonly SessionService _session;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(IHttpClientFactory factory, SessionService session)
    {
        _factory = factory;
        _session = session;
    }

    private HttpClient CreateAuthorizedClient()
    {
        var client = _factory.CreateClient("api");
        var token = _session.GetToken();
        if (token != null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // POST /api/auth/login
    public async Task<ServiceResponse<LoginResult>> LoginAsync(string email, string password)
    {
        try
        {
            var client = _factory.CreateClient("api");
            var body = JsonSerializer.Serialize(new { email, password });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/auth/login", content);
            var json = await response.Content.ReadAsStringAsync();

            var wrapped = JsonSerializer.Deserialize<ApiServiceResponse<LoginResult>>(json, JsonOptions);
            if (wrapped == null || !wrapped.Success)
                return new ServiceResponse<LoginResult> { Success = false, Message = wrapped?.Message ?? "Invalid credentials" };

            return new ServiceResponse<LoginResult> { Success = true, Data = wrapped.Data };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<LoginResult> { Success = false, Message = $"Login error: {ex.Message}" };
        }
    }

    // GET /api/eventos — returns plain array (not wrapped)
    public async Task<ServiceResponse<List<EventoResumen>>> GetEventosAsync()
    {
        try
        {
            var client = CreateAuthorizedClient();
            var response = await client.GetAsync("/api/eventos");
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new ServiceResponse<List<EventoResumen>> { Success = false, Message = "Could not load events" };

            var eventos = JsonSerializer.Deserialize<List<EventoResumen>>(json, JsonOptions);
            return new ServiceResponse<List<EventoResumen>> { Success = true, Data = eventos ?? new() };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<List<EventoResumen>> { Success = false, Message = ex.Message };
        }
    }

    // GET /api/eventos/{id} — returns plain object (not wrapped)
    public async Task<ServiceResponse<EventoDetalle>> GetEventoAsync(int id)
    {
        try
        {
            var client = CreateAuthorizedClient();
            var response = await client.GetAsync($"/api/eventos/{id}");
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new ServiceResponse<EventoDetalle> { Success = false, Message = "Event not found" };

            var evento = JsonSerializer.Deserialize<EventoDetalle>(json, JsonOptions);
            return new ServiceResponse<EventoDetalle> { Success = true, Data = evento };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<EventoDetalle> { Success = false, Message = ex.Message };
        }
    }

    // GET /api/eventos/{id}/asientos?soloDisponibles=true
    public async Task<ServiceResponse<List<EventoAsiento>>> GetAsientosAsync(int idEvento)
    {
        try
        {
            var client = CreateAuthorizedClient();
            var response = await client.GetAsync($"/api/eventos/{idEvento}/asientos?soloDisponibles=true");
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new ServiceResponse<List<EventoAsiento>> { Success = false, Message = "Could not load seats" };

            var asientos = JsonSerializer.Deserialize<List<EventoAsiento>>(json, JsonOptions);
            return new ServiceResponse<List<EventoAsiento>> { Success = true, Data = asientos ?? new() };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<List<EventoAsiento>> { Success = false, Message = ex.Message };
        }
    }

    // POST /api/ventas — endpoint pending implementation by API team
    public async Task<ServiceResponse<Sale>> CreateVentaAsync(SaleViewModel model)
    {
        try
        {
            var client = CreateAuthorizedClient();
            var employee = _session.GetEmployee();
            var body = JsonSerializer.Serialize(new
            {
                id_evento = model.EventoId,
                id_staff = employee?.IdStaff,
                asientos = model.AsientosSeleccionados,
                cliente = new
                {
                    nombre = model.CustomerName,
                    email = model.CustomerEmail,
                    documento = model.CustomerDocument,
                    telefono = model.CustomerPhone
                }
            });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/ventas", content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new ServiceResponse<Sale> { Success = false, Message = "Sale failed. Check seat availability." };

            var wrapped = JsonSerializer.Deserialize<ApiServiceResponse<Sale>>(json, JsonOptions);
            if (wrapped == null || !wrapped.Success)
                return new ServiceResponse<Sale> { Success = false, Message = wrapped?.Message ?? "Sale failed" };

            return new ServiceResponse<Sale> { Success = true, Data = wrapped.Data };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<Sale> { Success = false, Message = ex.Message };
        }
    }

    // GET /api/ventas/{id} — endpoint pending implementation by API team
    public async Task<ServiceResponse<Sale>> GetVentaAsync(int id)
    {
        try
        {
            var client = CreateAuthorizedClient();
            var response = await client.GetAsync($"/api/ventas/{id}");
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new ServiceResponse<Sale> { Success = false, Message = "Sale not found" };

            var wrapped = JsonSerializer.Deserialize<ApiServiceResponse<Sale>>(json, JsonOptions);
            if (wrapped == null || !wrapped.Success)
                return new ServiceResponse<Sale> { Success = false, Message = wrapped?.Message ?? "Sale not found" };

            return new ServiceResponse<Sale> { Success = true, Data = wrapped.Data };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<Sale> { Success = false, Message = ex.Message };
        }
    }
}