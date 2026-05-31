namespace events_tickets.Models;

public class CreateUserRequest
{
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Documento { get; set; } = "";
    public string? Telefono { get; set; }
}