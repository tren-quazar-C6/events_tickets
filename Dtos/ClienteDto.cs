using System.Text.Json.Serialization;

namespace events_tickets.Dtos;

public class ClienteDto
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public DateTime FechaRegistro { get; set; }
    public bool CuentaCreada { get; set; }
    [JsonIgnore]
    public string? PasswordTemporal { get; set; }
}
