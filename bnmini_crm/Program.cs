using bnmini_crm.Data;
using bnmini_crm.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ Scoped — правильный lifetime для DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<VenueHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<VenueHostedService>());
builder.Services.AddSingleton<ManagerAccessService>();

AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    Console.WriteLine($"FATAL: {e.ExceptionObject}");
};
TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    Console.WriteLine($"💥 UNOBSERVED TASK: {e.Exception}");
    e.SetObserved();
};


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapGet("/manager", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/manager.html");
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();