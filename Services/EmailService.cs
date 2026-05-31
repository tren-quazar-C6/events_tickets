using System.Net;
using System.Net.Mail;

namespace events_tickets.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public void SendAccountCredentials(string toEmail, string nombre, string password)
    {
        try
        {
            var smtp   = _config["Email:SmtpHost"]!;
            var port   = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var user   = _config["Email:User"]!;
            var pass   = _config["Email:Password"]!;
            var from   = _config["Email:From"]!;

            var body = $@"Hola {nombre},

Se creó tu cuenta para acceder a tus entradas en línea.

Email:      {toEmail}
Contraseña: {password}

Podés ingresar en nuestra plataforma con estas credenciales
para ver y descargar tus entradas en cualquier momento.

¡Gracias por tu compra!";

            using var client = new SmtpClient(smtp, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(user, pass)
            };

            var msg = new MailMessage(from, toEmail, "Tu cuenta y entradas", body);
            client.Send(msg);
        }
        catch
        {
            // Email failure must not abort the sale
        }
    }
}