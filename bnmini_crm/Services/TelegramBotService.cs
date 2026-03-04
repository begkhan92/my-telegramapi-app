using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using bnmini_crm.Data;
using bnmini_crm.Models;

namespace bnmini_crm.Services;

public class TelegramBotService
{
    public TelegramBotClient BotClient => _botClient;
    private readonly TelegramBotClient _botClient;
    private readonly AppDbContext _db;
    private readonly int _venueId;
    private readonly string _webAppUrl;

    public TelegramBotService(string token, int venueId, string webAppUrl, AppDbContext db)
    {
        _botClient = new TelegramBotClient(token);
        _venueId = venueId;
        _webAppUrl = webAppUrl;
        _db = db;
    }

    public async Task HandleUpdateAsync(Update update)
    {
        if (update.Message == null) return;

        long chatId = update.Message.Chat.Id;
        string text = update.Message.Text ?? "";

        if (text == "/start")
        {
            var from = update.Message.From!;

            // Найти или создать AppUser
            var user = _db.AppUsers.FirstOrDefault(u => u.TelegramUserId == chatId);
            if (user == null)
            {
                user = new AppUser
                {
                    TelegramUserId = chatId,
                    Username = from.Username,
                    FirstName = from.FirstName
                };
                _db.AppUsers.Add(user);
                await _db.SaveChangesAsync();
            }

            // Привязать к заведению если ещё не привязан
            var venueUser = _db.VenueUsers
                .FirstOrDefault(vu => vu.VenueId == _venueId && vu.AppUserId == user.Id);

            if (venueUser == null)
            {
                _db.VenueUsers.Add(new VenueUser
                {
                    VenueId = _venueId,
                    AppUserId = user.Id,
                    Role = VenueRole.Customer
                });
                await _db.SaveChangesAsync();
            }

            var venue = _db.Venues.FirstOrDefault(v => v.Id == _venueId);

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"👋 Добро пожаловать в {venue?.Name ?? "наш магазин"}!",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithWebApp(
                        "🛒 Открыть меню",
                        new WebAppInfo { Url = $"{_webAppUrl}/items?venueId={_venueId}" }
                    )
                )
            );
        }
        else if (!string.IsNullOrEmpty(text))
        {
            await _botClient.SendTextMessageAsync(chatId, "Нажми /start чтобы открыть меню.");
        }
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        await _botClient.SendTextMessageAsync(chatId, text);
    }

    public void StartPolling()
    {
        var cts = new CancellationTokenSource();
        _botClient.StartReceiving(
            async (bot, update, token) => await HandleUpdateAsync(update),
            async (bot, exception, token) =>
            {
                Console.WriteLine($"Error: {exception.Message}");
                if (exception.InnerException != null)
                    Console.WriteLine($"Inner: {exception.InnerException.Message}");
            },
            new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: cts.Token
        );
        Console.WriteLine($"Bot polling started for VenueId={_venueId}");
    }
}