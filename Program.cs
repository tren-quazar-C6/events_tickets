using events_tickets.Configuration;
using events_tickets.Infrastructure;
using events_tickets.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();

// Mongo audit logging (existing)
builder.Services.Configure<MongoDbOptions>(
    builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbOptions>>().Value;
    return new MongoClient(opts.ConnectionString);
});
builder.Services.AddSingleton<IAuditLogService, MongoAuditLogService>();

// PostgreSQL for main app data
var dbCs = builder.Configuration.GetConnectionString("MySQL")
           ?? throw new InvalidOperationException("Missing 'MySQL' connection string");
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new MySqlConnectionFactory(dbCs));

// Print server
builder.Services.Configure<PrintServerOptions>(
    builder.Configuration.GetSection(PrintServerOptions.SectionName));
builder.Services.AddHttpClient();

// Domain services
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IPrintService, PrintService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseMiddleware<events_tickets.Middleware.RequestTracingMiddleware>();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapStaticAssets();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();