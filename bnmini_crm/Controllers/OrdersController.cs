using Microsoft.AspNetCore.Mvc;
using bnmini_crm.Data;
using bnmini_crm.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using bnmini_crm.Services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types.ReplyMarkups;


namespace bnmini_crm.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly VenueHostedService _venueHosted;
    private readonly IServiceProvider _serviceProvider;

    public OrdersController(AppDbContext db, VenueHostedService venueHosted, IServiceProvider serviceProvider)
    {
        _db = db;
        _venueHosted = venueHosted;
        _serviceProvider = serviceProvider;
    }

    public record OrderItemDto(int ItemId, int Quantity);
    public record CreateOrderDto(int VenueId, int AppUserId, string Phone, string Address, string DeliveryTime, string RequestId, List<OrderItemDto> OrderItems);
    public record DeleteAddressDto(int AppUserId, string Address);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        // Защита от дублирования
        if (!string.IsNullOrEmpty(dto.RequestId))
        {
            var duplicate = await _db.Orders.AnyAsync(o => o.RequestId == dto.RequestId);
            if (duplicate) return Ok();
        }

        var user = await _db.AppUsers.FindAsync(dto.AppUserId);
        if (user != null && !string.IsNullOrEmpty(dto.Phone))
            user.Phone = dto.Phone;

        if (!string.IsNullOrEmpty(dto.Address))
        {
            var exists = await _db.DeliveryAddresses
                .AnyAsync(a => a.AppUserId == dto.AppUserId && a.Label == dto.Address);
            if (!exists)
                _db.DeliveryAddresses.Add(new DeliveryAddress { AppUserId = dto.AppUserId, Label = dto.Address });
        }

        var itemIds = dto.OrderItems.Select(i => i.ItemId).ToList();
        var items = await _db.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync();

        var order = new Order
        {
            VenueId = dto.VenueId,
            AppUserId = dto.AppUserId,
            Status = OrderStatus.New,
            Phone = dto.Phone,
            DeliveryAddress = dto.Address,
            DeliveryTime = dto.DeliveryTime,
            RequestId = dto.RequestId,
            OrderItems = dto.OrderItems.Select(i => new OrderItem
            {
                ItemId = i.ItemId,
                Quantity = i.Quantity
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // ✅ Сразу отвечаем клиенту — не ждём Telegram
        await SendTelegramNotificationsAsync(order, user, items, dto);
        //_ = Task.Run(() => SendTelegramNotificationsAsync(order, user, items, dto));

        return Ok();
    }

    private async Task SendTelegramNotificationsAsync(Order order, AppUser? user, List<Item> items, CreateOrderDto dto)
    {

        var venue = await _db.Venues.FindAsync(dto.VenueId);
        var tx = BotLocalization.Get(venue?.Language ?? "en");
        var currency = venue?.Currency ?? "USD";
        var deliveryFee = venue?.DeliveryFee ?? 15m;

        var bot = _venueHosted.GetBot(dto.VenueId);
        if (user == null || bot == null)
        {
            Console.WriteLine($"⚠️ bot={bot != null}, user={user != null}");
            return;
        }
        var itemsTotal = dto.OrderItems.Sum(oi =>
        {
            var item = items.FirstOrDefault(i => i.Id == oi.ItemId);
            return item != null ? item.Price * oi.Quantity : 0;
        });
        var grandTotal = itemsTotal + deliveryFee;

        var lines = dto.OrderItems.Select(oi =>
        {
            var item = items.FirstOrDefault(i => i.Id == oi.ItemId);
            if (item == null) return "";
            var subtotal = item.Price * oi.Quantity;
            return oi.Quantity > 1
                ? $"• {item.Name} x{oi.Quantity} — {subtotal:0.00} м"
                : $"• {item.Name} — {subtotal:0.00} м";
        }).Where(l => l != "").ToList();

        // Сообщение клиенту
        var clientMessage = string.Format(tx.OrderAccepted,
            order.Id,
            string.Join("\n", lines),
            $"{deliveryFee:0.00} {currency}",
            $"{grandTotal:0.00} {currency}",
            dto.Address,
            dto.DeliveryTime,
            dto.Phone);

        try
        {
            var sentMessage = await bot.SendMessage(
                chatId: user.TelegramUserId,
                text: clientMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown
            );

            // Отдельный DbContext для фонового потока!
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbOrder = await db.Orders.FindAsync(order.Id);
            if (dbOrder != null)
            {
                dbOrder.TelegramMessageId = sentMessage.MessageId;
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка отправки клиенту {user.TelegramUserId}: {ex.Message}");
        }

        // Сообщение операторам
        using var scope2 = _serviceProvider.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

        var operators = await db2.VenueUsers
            .Include(vu => vu.AppUser)
            .Where(vu => vu.VenueId == dto.VenueId && vu.Role == VenueRole.Operator)
            .ToListAsync();

        Console.WriteLine($"👥 Найдено операторов: {operators.Count}");

        var operatorMessage = string.Format(tx.NewOrder,
            order.Id,
            string.Join("\n", lines),
            $"{grandTotal:0.00} {currency}",
            dto.Address,
            dto.DeliveryTime,
            dto.Phone,
            user.FirstName + (user.Username != null ? $" (@{user.Username})" : ""));

        foreach (var op in operators)
        {
            try
            {
                var opMessage = await bot.SendMessage(
                    chatId: op.AppUser.TelegramUserId,
                    text: operatorMessage,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("✅ Подтвердить", $"confirm_{order.Id}"),
                            InlineKeyboardButton.WithCallbackData("❌ Отменить", $"cancel_{order.Id}")
                        }
                    })
                );

                var dbOrder2 = await db2.Orders.FindAsync(order.Id);
                if (dbOrder2 != null)
                {
                    dbOrder2.OperatorMessageId = opMessage.MessageId;
                    await db2.SaveChangesAsync();
                }
                Console.WriteLine($"✅ Оператор {op.AppUser.TelegramUserId} уведомлён.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка оператору {op.AppUser.TelegramUserId}: {ex.Message}");
            }
        }
    }

    [HttpGet("client-info/{appUserId}")]
    public async Task<IActionResult> GetClientInfo(int appUserId)
    {
        var user = await _db.AppUsers.FindAsync(appUserId);
        if (user == null) return NotFound();
        var addresses = await _db.DeliveryAddresses
            .Where(a => a.AppUserId == appUserId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => a.Label)
            .ToListAsync();
        return Ok(new { phone = user.Phone ?? "", addresses });
    }

    [HttpDelete("delete-address")]
    public async Task<IActionResult> DeleteAddress([FromBody] DeleteAddressDto dto)
    {
        var addr = await _db.DeliveryAddresses
            .FirstOrDefaultAsync(a => a.AppUserId == dto.AppUserId && a.Label == dto.Address);
        if (addr == null) return NotFound();
        _db.DeliveryAddresses.Remove(addr);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("categories/{venueId}")]
    public async Task<IActionResult> GetCategories(int venueId)
    {
        var cats = await _db.Categories
            .Where(c => c.VenueId == venueId)
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Name, c.IsDefault })
            .ToListAsync();
        return Ok(cats);
    }
}