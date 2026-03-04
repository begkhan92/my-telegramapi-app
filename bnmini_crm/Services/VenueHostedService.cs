using bnmini_crm.Data;
using bnmini_crm.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace bnmini_crm.Services;

public class VenueHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly Dictionary<int, TelegramBotService> _runningBots = new();

    public VenueHostedService(IServiceProvider services, IConfiguration config)
    {
        _services = services;
        _config = config;
    }

    public TelegramBotClient? GetBot(int venueId)
    {
        if (_runningBots.TryGetValue(venueId, out var botService))
            return botService.BotClient;
        return null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var webAppUrl = _config["WebAppUrl"] ?? "";

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var venues = await db.Venues
                .Where(v => !string.IsNullOrEmpty(v.TelegramBotToken))
                .ToListAsync(stoppingToken);

            foreach (var venue in venues)
            {
                // Запустить бота только если ещё не запущен
                if (!_runningBots.ContainsKey(venue.Id))
                {
                    var scopeForBot = _services.CreateScope();
                    var dbForBot = scopeForBot.ServiceProvider.GetRequiredService<AppDbContext>();

                    var bot = new TelegramBotService(
                        venue.TelegramBotToken,
                        venue.Id,
                        webAppUrl,
                        dbForBot
                    );
                    bot.StartPolling();
                    _runningBots[venue.Id] = bot;

                    Console.WriteLine($"✅ Бот запущен для заведения: {venue.Name}");
                }
            }

            // Проверять каждые 30 секунд
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}