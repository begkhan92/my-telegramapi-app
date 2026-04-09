using bnmini_crm.Data;
using bnmini_crm.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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

    public TelegramBotService? GetBotService(int venueId)
    {
        _runningBots.TryGetValue(venueId, out var botService);
        return botService;
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
                if (string.IsNullOrEmpty(venue.TelegramBotToken) ||
                    venue.TelegramBotToken.StartsWith("test_"))
                    continue;

                if (!_runningBots.ContainsKey(venue.Id))
                {
                    // Передаём IServiceProvider — бот сам создаёт scope при каждом update
                    var managerToken = _config["ManagerToken"] ?? "";
                    var accessService = _services.GetRequiredService<ManagerAccessService>();
                    var bot = new TelegramBotService(
                        venue.TelegramBotToken,
                        venue.Id,
                        webAppUrl,
                        _services,
                        managerToken,
                        accessService
                    );
                    bot.StartPolling();
                    _runningBots[venue.Id] = bot;
                    Console.WriteLine($"✅ Бот запущен для заведения: {venue.Name}");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        // Проверка незакрытых заказов в конце дня
        var lastCheck = DateTime.UtcNow.Date;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Asia/Ashgabat"));

            // Отправляем в 21:00 по туркменскому времени
            if (now.Hour == 21 && now.Date > lastCheck)
            {
                lastCheck = now.Date;
                await CheckUndeliveredOrders(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CheckUndeliveredOrders(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ashgabat");
        var today = DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date,
            DateTimeKind.Utc);

        var venues = await db.Venues
            .Where(v => !string.IsNullOrEmpty(v.TelegramBotToken) && !v.TelegramBotToken.StartsWith("test_"))
            .ToListAsync(stoppingToken);

        foreach (var venue in venues)
        {
            if (!_runningBots.TryGetValue(venue.Id, out var botService)) continue;

            var undelivered = await db.Orders
                .Where(o => o.VenueId == venue.Id
                    && o.CreatedAt >= today
                    && o.Status != OrderStatus.Delivered
                    && o.Status != OrderStatus.Cancelled)
                .ToListAsync(stoppingToken);

            if (!undelivered.Any()) continue;

            var operators = await db.VenueUsers
                .Include(vu => vu.AppUser)
                .Where(vu => vu.VenueId == venue.Id &&
                    (vu.Role == VenueRole.Operator || vu.Role == VenueRole.Boss))
                .ToListAsync(stoppingToken);

            var lines = string.Join("\n", undelivered.Select(o =>
                $"• Заказ #{o.Id} — {o.Status switch
                {
                    OrderStatus.New => "🆕 Новый",
                    OrderStatus.Confirmed => "✅ Подтверждён",
                    OrderStatus.InTransit => "🚗 В пути",
                    _ => "?"
                }}"));

            var msg = $"⚠️ *{venue.Name}* — незакрытые заказы за сегодня:\n\n{lines}\n\n" +
                      $"Всего: {undelivered.Count} заказов";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "✅ Все доставлены",
                    $"all_delivered_{venue.Id}_{today:yyyyMMdd}")
            }
        });

            foreach (var op in operators)
            {
                try
                {
                    await botService.BotClient.SendMessage(
                        chatId: op.AppUser.TelegramUserId,
                        text: msg,
                        parseMode: ParseMode.Markdown,
                        replyMarkup: keyboard
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка уведомления: {ex.Message}");
                }
            }
        }
    }
}