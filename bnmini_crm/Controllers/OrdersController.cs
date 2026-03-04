using Microsoft.AspNetCore.Mvc;
using bnmini_crm.Data;
using bnmini_crm.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using bnmini_crm.Services;

namespace bnmini_crm.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly VenueHostedService _venueHosted;

    public OrdersController(AppDbContext db, VenueHostedService venueHosted)
    {
        _db = db;
        _venueHosted = venueHosted;
    }

    public record OrderItemDto(int ItemId, int Quantity);
    public record CreateOrderDto(int VenueId, int AppUserId, string Phone, string Address, List<OrderItemDto> OrderItems);
    public record DeleteAddressDto(int AppUserId, string Address);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        // Сохранить телефон в профиль
        var user = await _db.AppUsers.FindAsync(dto.AppUserId);
        if (user != null && !string.IsNullOrEmpty(dto.Phone))
            user.Phone = dto.Phone;

        // Сохранить адрес если новый
        if (!string.IsNullOrEmpty(dto.Address))
        {
            var exists = await _db.DeliveryAddresses
                .AnyAsync(a => a.AppUserId == dto.AppUserId && a.Label == dto.Address);
            if (!exists)
                _db.DeliveryAddresses.Add(new DeliveryAddress { AppUserId = dto.AppUserId, Label = dto.Address });
        }

        // Загрузить товары для подсчёта
        var itemIds = dto.OrderItems.Select(i => i.ItemId).ToList();
        var items = await _db.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync();

        var order = new Order
        {
            VenueId = dto.VenueId,
            AppUserId = dto.AppUserId,
            Status = OrderStatus.New,
            Phone = dto.Phone,
            DeliveryAddress = dto.Address,
            OrderItems = dto.OrderItems.Select(i => new OrderItem
            {
                ItemId = i.ItemId,
                Quantity = i.Quantity
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Отправить сообщение клиенту
        if (user != null)
        {
            const decimal deliveryFee = 15m;
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
                    ? $"• {item.Name} x{oi.Quantity} — {subtotal:0.00} ₽"
                    : $"• {item.Name} — {subtotal:0.00} ₽";
            }).Where(l => l != "");

            var message =
                $"✅ *Ваш заказ принят!*\n\n" +
                string.Join("\n", lines) +
                $"\n\n" +
                $"🚚 Доставка — {deliveryFee:0.00} ₽\n" +
                $"💰 Итого — *{grandTotal:0.00} ₽*\n\n" +
                $"📍 Адрес: {dto.Address}\n" +
                $"📞 Телефон: {dto.Phone}";

            var bot = _venueHosted.GetBot(dto.VenueId);
            if (bot != null)
            {
                await bot.SendTextMessageAsync(
                    chatId: user.TelegramUserId,
                    text: message,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown
                );
            }
        }

        return Ok();
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