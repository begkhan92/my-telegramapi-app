using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using bnmini_crm.Data;
using bnmini_crm.Models;
using Microsoft.EntityFrameworkCore;

namespace bnmini_crm.Services;

public class TelegramBotService
{
    private readonly TelegramBotClient _botClient;
    public TelegramBotClient BotClient => _botClient;
    private readonly IServiceProvider _services;
    private readonly int _venueId;
    private readonly string _webAppUrl;
    private readonly string _managerToken;
    private readonly ManagerAccessService _accessService;
    private readonly Dictionary<long, int> _pendingCancellations = new();

    public TelegramBotService(string token, int venueId, string webAppUrl, IServiceProvider services, string managerToken, ManagerAccessService accessService)
    {
        _botClient = new TelegramBotClient(token);
        _venueId = venueId;
        _webAppUrl = webAppUrl;
        _services = services;
        _managerToken = managerToken;
        _accessService = accessService;
    }

    //private async Task SendOperatorKeyboard(AppDbContext db, long chatId)
    //{
    //    var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ashgabat");
    //    var today = DateTime.SpecifyKind(
    //        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date,
    //        DateTimeKind.Utc);

    //    var newCount = await db.Orders.CountAsync(o => o.VenueId == _venueId && o.CreatedAt >= today && o.Status == OrderStatus.New);
    //    var transitCount = await db.Orders.CountAsync(o => o.VenueId == _venueId && o.CreatedAt >= today && o.Status == OrderStatus.InTransit);
    //    var allCount = await db.Orders.CountAsync(o => o.VenueId == _venueId && o.CreatedAt >= today && o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled);

    //    var keyboard = new ReplyKeyboardMarkup(new[]
    //    {
    //    new[]
    //    {
    //        new KeyboardButton($"📋 Все активные ({allCount})"),
    //    },
    //    new[]
    //    {
    //        new KeyboardButton($"🆕 Новые ({newCount})"),
    //        new KeyboardButton($"🚗 В пути ({transitCount})")
    //    }
    //})
    //    {
    //        ResizeKeyboard = true,
    //        IsPersistent = true
    //    };

    //    await _botClient.SendMessage(
    //        chatId: chatId,
    //        text: "📊 Обновлено",
    //        replyMarkup: keyboard
    //    );
    //}

    private async Task SendOrdersList(AppDbContext db, long chatId, OrderStatus? filterStatus)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ashgabat");
        var today = DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date,
            DateTimeKind.Utc);

        var query = db.Orders
            .Include(o => o.AppUser)
            .Where(o => o.VenueId == _venueId && o.CreatedAt >= today);

        if (filterStatus.HasValue)
            query = query.Where(o => o.Status == filterStatus.Value);
        else
            query = query.Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled);

        var orders = await query.OrderBy(o => o.CreatedAt).ToListAsync();

        if (!orders.Any())
        {
            await _botClient.SendMessage(chatId: chatId, text: "📋 Заказов нет.");
            return;
        }

        // Считаем статистику
        var allToday = await db.Orders.CountAsync(o => o.VenueId == _venueId && o.CreatedAt >= today);
        var newCount = await db.Orders.CountAsync(o => o.VenueId == _venueId && o.CreatedAt >= today && o.Status == OrderStatus.New);
        var transitCount = await db.Orders.CountAsync(o => o.VenueId == _venueId && o.CreatedAt >= today && o.Status == OrderStatus.InTransit);

        var header = $"📋 *Заказы сегодня: {allToday}*\n" +
                     $"🆕 Ожидают: {newCount} | 🚗 В пути: {transitCount}\n\n";

        // Строим кнопки — каждый заказ отдельная кнопка
        var buttons = orders.Select(o =>
        {
            var statusEmoji = o.Status switch
            {
                OrderStatus.New => "🆕",
                OrderStatus.Confirmed => "✅",
                OrderStatus.InTransit => "🚗",
                OrderStatus.Delivered => "📦",
                OrderStatus.Cancelled => "❌",
                _ => "?"
            };
            var name = o.AppUser?.FirstName ?? "?";
            var label = $"{statusEmoji} #{o.Id} — {name} — {o.DeliveryTime}";
            return new[] { InlineKeyboardButton.WithCallbackData(label, $"next_status_{o.Id}") };
        }).ToArray();

        await _botClient.SendMessage(
            chatId: chatId,
            text: header + $"Нажмите на заказ чтобы перейти к следующему статусу:",
            parseMode: ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(buttons)
        );
    }

    public async Task HandleUpdateAsync(Update update)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == _venueId);
        var tx = BotLocalization.Get(venue?.Language ?? "en");
        var currency = venue?.Currency ?? "USD";
        var deliveryFee = venue?.DeliveryFee ?? 0;
        var managerPassword = venue?.ManagerPassword ?? _managerToken;
        var managerUrl = $"{_webAppUrl}/manager.html?venueId={_venueId}&token={Uri.EscapeDataString(managerPassword)}";

        // ── CallbackQuery ──────────────────────────────────────
        if (update.CallbackQuery != null)
        {
            var query = update.CallbackQuery;
            var callbackChatId = query.Message!.Chat.Id;
            var messageId = query.Message.MessageId;
            await _botClient.AnswerCallbackQuery(query.Id);

            var cbUser = db.AppUsers.FirstOrDefault(u => u.TelegramUserId == callbackChatId);
            var cbVenueUser = cbUser != null
                ? db.VenueUsers.FirstOrDefault(vu => vu.VenueId == _venueId && vu.AppUserId == cbUser.Id)
                : null;

            // Boss кнопки
            if (cbVenueUser?.Role == VenueRole.Boss)
            {
                switch (query.Data)
                {
                    case "boss_revenue": await SendRevenueReport(db, callbackChatId, tx, currency, managerUrl); return;
                    case "boss_items_revenue": await SendItemRevenueReport(db, callbackChatId, tx, currency, managerUrl); return;
                    case "boss_catalog": await SendCatalogReport(db, callbackChatId, tx, currency, managerUrl); return;
                    case "boss_clients": await SendClientsReport(db, callbackChatId, tx, currency, managerUrl); return;
                    case "boss_orders": await SendOrdersReport(db, callbackChatId, tx, managerUrl); return;
                }
            }

            // Оператор — подтверждение заказа
            if (query.Data?.StartsWith("confirm_") == true)
            {
                var orderId = int.Parse(query.Data.Replace("confirm_", ""));
                var order = db.Orders.FirstOrDefault(o => o.Id == orderId && o.VenueId == _venueId);
                if (order != null && order.Status == OrderStatus.New)
                {
                    order.Status = OrderStatus.Confirmed;
                    await db.SaveChangesAsync();

                    // Уведомить клиента
                    var client = db.AppUsers.FirstOrDefault(u => u.Id == order.AppUserId);
                    if (client != null)
                    {
                        await _botClient.SendMessage(
                            chatId: client.TelegramUserId,
                            text: $"✅ *Ваш заказ #{order.Id} подтверждён!*\nМы начали готовить 👨‍🍳",
                            parseMode: ParseMode.Markdown
                        );
                    }

                    // Обновить кнопки у оператора
                    await _botClient.EditMessageReplyMarkup(
                        chatId: callbackChatId,
                        messageId: messageId,
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🚗 В пути", $"transit_{orderId}"),
                        InlineKeyboardButton.WithCallbackData("❌ Отменить", $"cancel_{orderId}")
                    }
                        })
                    );

                    await _botClient.SendMessage(chatId: callbackChatId, text: $"✅ Заказ #{orderId} подтверждён.");
                }
                return;
            }

            // Оператор — в пути
            if (query.Data?.StartsWith("transit_") == true)
            {
                var orderId = int.Parse(query.Data.Replace("transit_", ""));
                var order = db.Orders.FirstOrDefault(o => o.Id == orderId && o.VenueId == _venueId);
                if (order != null && order.Status == OrderStatus.Confirmed)
                {
                    order.Status = OrderStatus.InTransit;
                    await db.SaveChangesAsync();

                    var client = db.AppUsers.FirstOrDefault(u => u.Id == order.AppUserId);
                    if (client != null)
                    {
                        await _botClient.SendMessage(
                            chatId: client.TelegramUserId,
                            text: $"🚗 *Ваш заказ #{order.Id} в пути!*\nОжидайте доставку 🛵",
                            parseMode: ParseMode.Markdown
                        );
                    }

                    await _botClient.EditMessageReplyMarkup(
                        chatId: callbackChatId,
                        messageId: messageId,
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Доставлен", $"delivered_{orderId}"),
                        InlineKeyboardButton.WithCallbackData("❌ Отменить", $"cancel_{orderId}")
                    }
                        })
                    );

                    await _botClient.SendMessage(chatId: callbackChatId, text: $"🚗 Заказ #{orderId} в пути.");
                }
                return;
            }

            // Оператор — доставлен
            if (query.Data?.StartsWith("delivered_") == true)
            {
                var orderId = int.Parse(query.Data.Replace("delivered_", ""));
                var order = db.Orders.FirstOrDefault(o => o.Id == orderId && o.VenueId == _venueId);
                if (order != null && order.Status == OrderStatus.InTransit)
                {
                    order.Status = OrderStatus.Delivered;
                    await db.SaveChangesAsync();

                    var client = db.AppUsers.FirstOrDefault(u => u.Id == order.AppUserId);
                    if (client != null)
                    {
                        await _botClient.SendMessage(
                            chatId: client.TelegramUserId,
                            text: $"✅ *Ваш заказ #{order.Id} доставлен!*\nПриятного аппетита! 🍽",
                            parseMode: ParseMode.Markdown
                        );
                    }

                    await _botClient.EditMessageReplyMarkup(
                        chatId: callbackChatId,
                        messageId: messageId,
                        replyMarkup: null
                    );

                    await _botClient.SendMessage(chatId: callbackChatId, text: $"✅ Заказ #{orderId} доставлен.");
                }
                return;
            }

            // Оператор — отмена
            if (query.Data?.StartsWith("cancel_") == true)
            {
                var orderId = int.Parse(query.Data.Replace("cancel_", ""));
                var order = db.Orders.FirstOrDefault(o => o.Id == orderId && o.VenueId == _venueId);
                if (order != null && order.Status != OrderStatus.Cancelled && order.Status != OrderStatus.Delivered)
                {
                    // Сохраняем orderId в ожидании причины
                    _pendingCancellations[callbackChatId] = orderId;

                    await _botClient.SendMessage(
                        chatId: callbackChatId,
                        text: $"❌ Укажите причину отмены заказа #{orderId}:"
                    );
                }
                return;
            }

            if (query.Data?.StartsWith("all_delivered_") == true)
            {
                var parts = query.Data.Split('_');
                // all_delivered_{venueId}_{date}
                var venueId = int.Parse(parts[2]);
                var date = DateTime.ParseExact(parts[3], "yyyyMMdd", null);
                var dateUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc);

                var orders = db.Orders
                    .Where(o => o.VenueId == venueId
                        && o.CreatedAt >= dateUtc
                        && o.Status != OrderStatus.Delivered
                        && o.Status != OrderStatus.Cancelled)
                    .ToList();

                foreach (var o in orders)
                    o.Status = OrderStatus.Delivered;

                await db.SaveChangesAsync();

                await _botClient.EditMessageReplyMarkup(
                    chatId: callbackChatId,
                    messageId: messageId,
                    replyMarkup: null
                );

                await _botClient.SendMessage(
                    chatId: callbackChatId,
                    text: $"✅ {orders.Count} заказов отмечены как доставленные."
                );
                return;
            }

            if (query.Data?.StartsWith("next_status_") == true)
            {
                var orderId = int.Parse(query.Data.Replace("next_status_", ""));
                var order = db.Orders
                    .Include(o => o.AppUser)
                    .FirstOrDefault(o => o.Id == orderId && o.VenueId == _venueId);

                if (order == null) return;

                string statusMsg = "";
                string clientMsg = "";

                switch (order.Status)
                {
                    case OrderStatus.New:
                        order.Status = OrderStatus.Confirmed;
                        statusMsg = $"✅ Заказ #{orderId} подтверждён.";
                        clientMsg = $"✅ *Ваш заказ #{orderId} подтверждён!*\nМы начали готовить 👨‍🍳";
                        break;
                    case OrderStatus.Confirmed:
                        order.Status = OrderStatus.InTransit;
                        statusMsg = $"🚗 Заказ #{orderId} в пути.";
                        clientMsg = $"🚗 *Ваш заказ #{orderId} в пути!*\nОжидайте доставку 🛵";
                        break;
                    case OrderStatus.InTransit:
                        order.Status = OrderStatus.Delivered;
                        statusMsg = $"📦 Заказ #{orderId} доставлен.";
                        clientMsg = $"📦 *Ваш заказ #{orderId} доставлен!*\nПриятного аппетита! 🍽";
                        break;
                    default:
                        await _botClient.AnswerCallbackQuery(query.Id, "Заказ уже закрыт");
                        return;
                }

                await db.SaveChangesAsync();

                // Уведомить клиента
                var client = db.AppUsers.FirstOrDefault(u => u.Id == order.AppUserId);
                if (client != null)
                {
                    try
                    {
                        await _botClient.SendMessage(
                            chatId: client.TelegramUserId,
                            text: clientMsg,
                            parseMode: ParseMode.Markdown
                        );
                    }
                    catch { }
                }

                await _botClient.AnswerCallbackQuery(query.Id, statusMsg);

                // Обновить список
                await SendOrdersList(db, callbackChatId, null);
                //await SendOperatorKeyboard(db, callbackChatId);
                return;
            }

            // About
            if (query.Data == "about")
            {
                var about = venue?.About ?? "ℹ️";
                await _botClient.SendMessage(chatId: callbackChatId, text: about, parseMode: ParseMode.Markdown);
            }
            return;
        }

        if (update.Message == null) return;

        long chatId = update.Message.Chat.Id;
        string text = update.Message.Text ?? "";

        var venueData = db.Venues.FirstOrDefault(v => v.Id == _venueId);
        var user = db.AppUsers.FirstOrDefault(u => u.TelegramUserId == chatId);
        var venueUser = user != null
            ? db.VenueUsers.FirstOrDefault(vu => vu.VenueId == _venueId && vu.AppUserId == user.Id)
            : null;
        var role = venueUser?.Role;

        // ── Reply оператора ────────────────────────────────────
        if (update.Message.ReplyToMessage != null && role == VenueRole.Operator)
        {
            var replyText = text.ToLower().Trim();
            var confirmedWords = new[] { "ок", "ok", "подтверждено", "принято", "готово", "+", "да" };

            if (confirmedWords.Any(w => replyText.Contains(w)))
            {
                var repliedMessageId = update.Message.ReplyToMessage.MessageId;
                var order = db.Orders.FirstOrDefault(o =>
                    (o.TelegramMessageId == repliedMessageId || o.OperatorMessageId == repliedMessageId)
                    && o.VenueId == _venueId);

                if (order != null && order.Status == OrderStatus.New)
                {
                    order.Status = OrderStatus.Confirmed;
                    await db.SaveChangesAsync();

                    try
                    {
                        await _botClient.SetMessageReactionAsync(
                            chatId: chatId,
                            messageId: update.Message.ReplyToMessage.MessageId,
                            reaction: new List<ReactionType> { new ReactionTypeEmoji { Emoji = "👍" } }
                        );
                    }
                    catch (Exception ex) { Console.WriteLine($"Reaction error: {ex.Message}"); }

                    var client = db.AppUsers.FirstOrDefault(u => u.Id == order.AppUserId);
                    if (client != null)
                    {
                        await _botClient.SendMessage(
                            chatId: client.TelegramUserId,
                            text: string.Format(tx.OrderConfirmed, order.Id),
                            parseMode: ParseMode.Markdown
                        );
                    }

                    await _botClient.SendMessage(
                     chatId: chatId,
                     text: string.Format(tx.OrderConfirmedOperator, order.Id)
                 );
                    return;
                }
            }
        }

        // Ожидание причины отмены
        if (_pendingCancellations.TryGetValue(chatId, out var cancelOrderId))
        {
            _pendingCancellations.Remove(chatId);
            var order = db.Orders.FirstOrDefault(o => o.Id == cancelOrderId && o.VenueId == _venueId);
            if (order != null)
            {
                order.Status = OrderStatus.Cancelled;
                order.CancelReason = text;
                await db.SaveChangesAsync();

                var client = db.AppUsers.FirstOrDefault(u => u.Id == order.AppUserId);
                if (client != null)
                {
                    await _botClient.SendMessage(
                        chatId: client.TelegramUserId,
                        text: $"❌ *Ваш заказ #{order.Id} отменён.*\nПричина: {text}",
                        parseMode: ParseMode.Markdown
                    );
                }

                await _botClient.SendMessage(
                    chatId: chatId,
                    text: $"❌ Заказ #{cancelOrderId} отменён. Клиент уведомлён."
                );
            }
            return;
        }



        // /orders
        if (text == "/orders" && (role == VenueRole.Operator || role == VenueRole.Boss))
        {
            await SendOrdersList(db, chatId, null);
            return;
        }

        // /orders_new
        if (text == "/orders_new" && (role == VenueRole.Operator || role == VenueRole.Boss))
        {
            await SendOrdersList(db, chatId, OrderStatus.New);
            return;
        }

        // /orders_transit
        if (text == "/orders_transit" && (role == VenueRole.Operator || role == VenueRole.Boss))
        {
            await SendOrdersList(db, chatId, OrderStatus.InTransit);
            return;
        }

        // ── /start ─────────────────────────────────────────────
        if (text == "/start")
        {
            var from = update.Message.From!;

            if (user == null)
            {
                user = new AppUser { TelegramUserId = chatId, Username = from.Username, FirstName = from.FirstName };
                db.AppUsers.Add(user);
                await db.SaveChangesAsync();
            }

            if (venueUser == null)
            {
                venueUser = new VenueUser
                {
                    VenueId = _venueId,
                    AppUserId = user.Id,
                    Role = VenueRole.Customer,
                    JoinedAt = DateTime.UtcNow
                };
                db.VenueUsers.Add(venueUser);
                await db.SaveChangesAsync();
                role = VenueRole.Customer;
            }

            if (role == VenueRole.Boss)
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: string.Format(tx.BossWelcome, venueData?.Name ?? ""),
                    parseMode: ParseMode.Markdown,
                    replyMarkup: BossKeyboard(tx, managerUrl)
                );
                return;
            }

            if (role == VenueRole.Operator)
            {
                //await SendOperatorKeyboard(db, chatId);
                await _botClient.SendMessage(
                   chatId: chatId,
                   text: string.Format(tx.OperatorWelcome, venueData?.Name ?? ""),
                   parseMode: ParseMode.Markdown
               );
                return;
            }

            // Customer
            await _botClient.SendMessage(
                chatId: chatId,
                text: string.Format(tx.Welcome, venueData?.Name ?? ""),
                parseMode: ParseMode.Markdown,
                replyMarkup: CustomerKeyboard(tx, user.Id)
            );
            return;
        }

        // ── Любое другое сообщение ─────────────────────────────
        if (role == VenueRole.Boss)
        {
            await _botClient.SendMessage(chatId: chatId, text: tx.BossWelcome, replyMarkup: BossKeyboard(tx, managerUrl));
            return;
        }
        var appUser = db.AppUsers.FirstOrDefault(u => u.TelegramUserId == chatId);
        await _botClient.SendMessage(
            chatId: chatId,
            text: tx.PressStart,
            replyMarkup: CustomerKeyboard(tx, appUser?.Id ?? 0)
        );
    }

    // ── Клавиатуры ─────────────────────────────────────────────

    private InlineKeyboardMarkup BossKeyboard(BotTexts tx, string managerUrl) =>
        new InlineKeyboardMarkup(new[]
        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(tx.BtnRevenue, "boss_revenue"),
            InlineKeyboardButton.WithCallbackData(tx.BtnItemsRevenue, "boss_items_revenue")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(tx.BtnOrders, "boss_orders"),
            InlineKeyboardButton.WithCallbackData(tx.BtnClients, "boss_clients")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(tx.BtnCatalog, "boss_catalog")
        },
        new[]
        {
            InlineKeyboardButton.WithUrl(tx.BtnPanel, managerUrl)
        }
        });

    private InlineKeyboardMarkup CustomerKeyboard(BotTexts tx, int userId) =>
        new InlineKeyboardMarkup(new[]
        {
        new[]
        {
            InlineKeyboardButton.WithWebApp(
                tx.MakeOrder,
                new WebAppInfo { Url = $"{_webAppUrl}/items.html?venueId={_venueId}&userId={userId}&lang={tx.Lang ?? "en"}" }
            )
        },
        new[] { InlineKeyboardButton.WithCallbackData(tx.BtnAbout, "about") }
        });

    // ── Отчёты ─────────────────────────────────────────────────

    private async Task SendRevenueReport(AppDbContext db, long chatId, BotTexts tx, string currency, string managerUrl)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ashgabat");
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
        var venue = db.Venues.FirstOrDefault(v => v.Id == _venueId);

        var allOrders = await db.Orders
            .Where(o => o.VenueId == _venueId)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
            .ToListAsync();

        var confirmedOrders = allOrders.Where(o => o.Status == OrderStatus.Confirmed).ToList();

        var ordersTotal = allOrders.Count;
        var ordersToday = allOrders.Count(o => o.CreatedAt >= today);
        var confirmedTotal = confirmedOrders.Count;
        var confirmedToday = confirmedOrders.Count(o => o.CreatedAt >= today);

        var revenueTotal = confirmedOrders.Sum(o => o.OrderItems.Sum(oi => oi.Item != null ? oi.Item.Price * oi.Quantity : 0));
        var revenueToday = confirmedOrders.Where(o => o.CreatedAt >= today)
            .Sum(o => o.OrderItems.Sum(oi => oi.Item != null ? oi.Item.Price * oi.Quantity : 0));
        var avgCheck = confirmedTotal > 0 ? revenueTotal / confirmedTotal : 0;

        var msg = string.Format(tx.RevenueReport,
            venue?.Name,
            $"{revenueToday:0.00} {currency}",
            confirmedToday,
            $"{revenueTotal:0.00} {currency}",
            confirmedTotal,
            $"{avgCheck:0.00} {currency}");

        await _botClient.SendMessage(chatId: chatId, text: msg, parseMode: ParseMode.Markdown, replyMarkup: BossKeyboard(tx, managerUrl));
    }

    private async Task SendOrdersReport(AppDbContext db, long chatId, BotTexts tx, string managerUrl)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ashgabat");
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
        var venue = db.Venues.FirstOrDefault(v => v.Id == _venueId);

        var allOrders = await db.Orders.Where(o => o.VenueId == _venueId).ToListAsync();

        var totalAll = allOrders.Count;
        var totalConfirmed = allOrders.Count(o => o.Status == OrderStatus.Confirmed);
        var totalNew = allOrders.Count(o => o.Status == OrderStatus.New);
        var todayAll = allOrders.Count(o => o.CreatedAt >= today);
        var todayConfirmed = allOrders.Count(o => o.CreatedAt >= today && o.Status == OrderStatus.Confirmed);
        var todayNew = allOrders.Count(o => o.CreatedAt >= today && o.Status == OrderStatus.New);

        var msg = string.Format(tx.OrdersReport,
            venue?.Name,
            todayAll, todayConfirmed, todayNew,
            totalAll, totalConfirmed, totalNew);

        await _botClient.SendMessage(
            chatId: chatId,
            text: msg,
            parseMode: ParseMode.Markdown,
            replyMarkup: BossKeyboard(tx, managerUrl)
        );
    }

    private async Task SendItemRevenueReport(AppDbContext db, long chatId, BotTexts tx, string currency, string managerUrl)
    {
        var venue = db.Venues.FirstOrDefault(v => v.Id == _venueId);
        var confirmedOrders = await db.Orders
            .Where(o => o.VenueId == _venueId && o.Status == OrderStatus.Confirmed)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
            .ToListAsync();

        var revenueTotal = confirmedOrders.Sum(o => o.OrderItems.Sum(oi => oi.Item != null ? oi.Item.Price * oi.Quantity : 0));
        var itemRevenue = confirmedOrders.SelectMany(o => o.OrderItems)
            .Where(oi => oi.Item != null)
            .GroupBy(oi => oi.Item!.Name)
            .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity), Sum = g.Sum(x => x.Item!.Price * x.Quantity) })
            .OrderByDescending(x => x.Sum).ToList();

        if (!itemRevenue.Any())
        {
            await _botClient.SendMessage(chatId: chatId, text: tx.NoOrdersYet, replyMarkup: BossKeyboard(tx, managerUrl));
            return;
        }

        var lines = itemRevenue.Select(i => $"• {i.Name} ×{i.Qty} — {i.Sum:0.00} м");
        var msg = string.Format(tx.ItemsReport, venue?.Name, lines, $"{revenueTotal:0.00} {currency}");


        await _botClient.SendMessage(chatId: chatId, text: msg, parseMode: ParseMode.Markdown, replyMarkup: BossKeyboard(tx, managerUrl));
    }

    private async Task SendCatalogReport(AppDbContext db, long chatId, BotTexts tx, string currency, string managerUrl)
    {
        var venue = db.Venues.FirstOrDefault(v => v.Id == _venueId);
        var itemsCount = await db.Items.CountAsync(i => i.VenueId == _venueId);
        var categoriesCount = await db.Categories.CountAsync(c => c.VenueId == _venueId);

        var msg = string.Format(tx.CatalogReport, venue?.Name, itemsCount, categoriesCount);

        await _botClient.SendMessage(chatId: chatId, text: msg, parseMode: ParseMode.Markdown, replyMarkup: BossKeyboard(tx, managerUrl));
    }

    private async Task SendClientsReport(AppDbContext db, long chatId, BotTexts tx, string currency, string managerUrl)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ashgabat");
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
        var venue = db.Venues.FirstOrDefault(v => v.Id == _venueId);
        var usersTotal = await db.VenueUsers.CountAsync(vu => vu.VenueId == _venueId && vu.Role == VenueRole.Customer);
        var usersToday = await db.VenueUsers.CountAsync(vu => vu.VenueId == _venueId && vu.Role == VenueRole.Customer && vu.JoinedAt >= today);

        var msg = string.Format(tx.ClientsReport, venue?.Name, usersToday, usersTotal);

        await _botClient.SendMessage(chatId: chatId, text: msg, parseMode: ParseMode.Markdown, replyMarkup: BossKeyboard(tx, managerUrl));
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        await _botClient.SendMessage(chatId, text, parseMode: ParseMode.Markdown);
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
            new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery } },
            cancellationToken: cts.Token
        );
        Console.WriteLine($"Bot polling started for VenueId={_venueId}");
    }
}