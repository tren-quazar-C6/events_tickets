var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<events_tickets.Configuration.MongoLoggingOptions>(
    builder.Configuration.GetSection(events_tickets.Configuration.MongoLoggingOptions.SectionName));
builder.Services.AddSingleton<MongoDB.Driver.IMongoClient>(serviceProvider =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<events_tickets.Configuration.MongoLoggingOptions>>()
        .Value;

    return new MongoDB.Driver.MongoClient(options.ConnectionString);
});
builder.Services.AddSingleton<events_tickets.Services.IAuditLogService, events_tickets.Services.MongoAuditLogService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseMiddleware<events_tickets.Middleware.RequestTracingMiddleware>();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
