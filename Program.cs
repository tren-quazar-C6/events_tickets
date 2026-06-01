using events_tickets.Configuration;
using events_tickets.Infrastructure;
using events_tickets.Services;
using Dapper;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddControllersWithViews();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient();

// App services
builder.Services.AddHttpContextAccessor();
var dbCs = builder.Configuration.GetConnectionString("MySQL")
           ?? throw new InvalidOperationException("Missing 'MySQL' connection string");
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new MySqlConnectionFactory(dbCs));

builder.Services.Configure<PrintServerOptions>(
    builder.Configuration.GetSection(PrintServerOptions.SectionName));
builder.Services.Configure<EmailOptions>(opt =>
{
    opt.Host = builder.Configuration["Email:Host"] ?? builder.Configuration["Email:SmtpHost"] ?? "";
    opt.Port = int.TryParse(builder.Configuration["Email:Port"] ?? builder.Configuration["Email:SmtpPort"], out var p) ? p : 587;
    opt.Username = builder.Configuration["Email:Username"] ?? builder.Configuration["Email:User"] ?? "";
    opt.Password = builder.Configuration["Email:Password"] ?? "";
    opt.FromAddress = builder.Configuration["Email:FromAddress"] ?? builder.Configuration["Email:From"] ?? "";
    opt.FromName = builder.Configuration["Email:FromName"] ?? "Taquilla";
    opt.EnableSsl = bool.TryParse(builder.Configuration["Email:EnableSsl"], out var ssl) ? ssl : true;
});

builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IPrintService, PrintService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// MongoDB audit logging
builder.Services.Configure<MongoLoggingOptions>(
    builder.Configuration.GetSection(MongoLoggingOptions.SectionName));
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoLoggingOptions>>().Value;
    return new MongoClient(options.ConnectionString);
});
builder.Services.AddSingleton<IAuditLogService, MongoAuditLogService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseMiddleware<events_tickets.Middleware.RequestTracingMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
