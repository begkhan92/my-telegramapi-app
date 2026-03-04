//using bnmini_crm.Data;
//using bnmini_crm.Services;
//using Microsoft.EntityFrameworkCore;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container
//builder.Services.AddControllers();
//builder.Services.AddRazorPages(); // Needed for _Host.cshtml
//builder.Services.AddServerSideBlazor(); // Needed for Blazor Server

//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//builder.Services.AddSingleton<TelegramBotService>();

//builder.Services.AddDbContext<AppDbContext>(opt =>
//    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")),
//    ServiceLifetime.Singleton);

//var app = builder.Build();

//// Configure middleware
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//// Serve static files (CSS, JS, etc.)
//app.UseStaticFiles();

//// Enable routing
//app.UseRouting();

//app.UseAuthorization();

//// Map API controllers
//app.MapControllers();

//// Map Blazor hub
//app.MapBlazorHub();

//// Map fallback to Blazor's _Host.cshtml for non-API routes
//app.MapFallbackToPage("/_Host");

//// Start Telegram bot
//var botService = app.Services.GetRequiredService<TelegramBotService>();
//if (app.Environment.IsDevelopment())
//{
//    botService.StartPolling();
//}

//app.Run();
using bnmini_crm.Data;
using bnmini_crm.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")),
    ServiceLifetime.Singleton);

// ✅ ДО builder.Build()
builder.Services.AddSingleton<VenueHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<VenueHostedService>());

builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    });

var app = builder.Build();

// Применить миграции
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

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