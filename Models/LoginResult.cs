namespace events_tickets.Models;

public class LoginResult
{
    public string? Token { get; set; }
    public DateTime ExpiraEn { get; set; }
    public int IdStaff { get; set; }
    public string? Nombre { get; set; }
    public string? Email { get; set; }
    public string? Rol { get; set; }
}