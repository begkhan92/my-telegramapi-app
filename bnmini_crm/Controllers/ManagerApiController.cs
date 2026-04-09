using Microsoft.AspNetCore.Mvc;
using bnmini_crm.Data;
using bnmini_crm.Models;
using Microsoft.EntityFrameworkCore;
using bnmini_crm.Services;

namespace bnmini_crm.Controllers;

[ApiController]
[Route("api/manager")]
public class ManagerApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public ManagerApiController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private bool CheckToken(string? token)
    {
        var valid = _config["ManagerToken"];
        Console.WriteLine($"🔑 CheckToken: got='{token}' expected='{valid}'");
        return !string.IsNullOrEmpty(token) && token == valid;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(int venueId, string token)
    {
        if (!CheckToken(token)) return Unauthorized();

        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ashgabat");
        var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
        var today = DateTime.SpecifyKind(todayLocal, DateTimeKind.Utc);

        var allOrders = await _db.Orders
            .Where(o => o.VenueId == venueId)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
            .ToListAsync();

        var confirmed = allOrders.Where(o => o.Status == OrderStatus.Confirmed).ToList();

        var revenueTotal = confirmed.Sum(o => o.OrderItems.Sum(oi => oi.Item != null ? oi.Item.Price * oi.Quantity : 0));
        var revenueToday = confirmed.Where(o => o.CreatedAt >= today)
            .Sum(o => o.OrderItems.Sum(oi => oi.Item != null ? oi.Item.Price * oi.Quantity : 0));
        var avgCheck = confirmed.Count > 0 ? revenueTotal / confirmed.Count : 0;

        var itemRevenue = confirmed.SelectMany(o => o.OrderItems)
            .Where(oi => oi.Item != null)
            .GroupBy(oi => oi.Item!.Name)
            .Select(g => new { name = g.Key, qty = g.Sum(x => x.Quantity), sum = g.Sum(x => x.Item!.Price * x.Quantity) })
            .OrderByDescending(x => x.sum).ToList();

        var topTimes = allOrders.GroupBy(o => o.CreatedAt.Hour)
            .Select(g => new { hour = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count).Take(5).ToList();

        var venue = await _db.Venues.FirstOrDefaultAsync(v => v.Id == venueId);

        return Ok(new
        {
            venueName = venue?.Name ?? "",
            ordersTotal = allOrders.Count,
            ordersToday = allOrders.Count(o => o.CreatedAt >= today),
            revenueTotal,
            revenueToday,
            avgCheck,
            usersTotal = await _db.VenueUsers.CountAsync(vu => vu.VenueId == venueId && vu.Role == VenueRole.Customer),
            usersToday = await _db.VenueUsers.CountAsync(vu => vu.VenueId == venueId && vu.Role == VenueRole.Customer && vu.JoinedAt >= today),
            itemsCount = await _db.Items.CountAsync(i => i.VenueId == venueId),
            categoriesCount = await _db.Categories.CountAsync(c => c.VenueId == venueId),
            itemRevenue,
            topTimes
        });
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetItems(int venueId, string token)
    {
        if (!CheckToken(token)) return Unauthorized();
        var items = await _db.Items
            .Where(i => i.VenueId == venueId)
            .Include(i => i.Images)
            .OrderBy(i => i.CategoryId).ThenBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Description,
                i.Price,
                i.ImageUrl,
                i.CategoryId,
                images = i.Images.OrderBy(img => img.SortOrder).Select(img => new { img.Id, img.Url }).ToList()
            })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(int venueId, string token)
    {
        if (!CheckToken(token)) return Unauthorized();
        var cats = await _db.Categories
            .Where(c => c.VenueId == venueId)
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Name, c.IsDefault, c.SortOrder })
            .ToListAsync();
        return Ok(cats);
    }

    public record SaveItemDto(int? Id, int VenueId, string Name, string? Description, decimal Price, int? CategoryId, string? ImageUrl, List<string>? ImageUrls);

    [HttpPost("save-item")]
    public async Task<IActionResult> SaveItem([FromBody] SaveItemDto dto, [FromQuery] string token)
    {
        if (!CheckToken(token)) return Unauthorized();

        if (dto.Id.HasValue && dto.Id > 0)
        {
            var item = await _db.Items.Include(i => i.Images).FirstOrDefaultAsync(i => i.Id == dto.Id.Value);
            if (item == null) return NotFound();

            item.Name = dto.Name.Trim();
            item.Description = dto.Description?.Trim() ?? "";
            item.Price = dto.Price;
            item.CategoryId = dto.CategoryId;
            item.ImageUrl = dto.ImageUrls?.FirstOrDefault();

            _db.ItemImages.RemoveRange(item.Images);
            if (dto.ImageUrls != null)
                for (int i = 0; i < dto.ImageUrls.Count; i++)
                    item.Images.Add(new ItemImage { Url = dto.ImageUrls[i], SortOrder = i });
        }
        else
        {
            var item = new Item
            {
                VenueId = dto.VenueId,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? "",
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                ImageUrl = dto.ImageUrls?.FirstOrDefault()
            };
            if (dto.ImageUrls != null)
                for (int i = 0; i < dto.ImageUrls.Count; i++)
                    item.Images.Add(new ItemImage { Url = dto.ImageUrls[i], SortOrder = i });
            _db.Items.Add(item);
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("delete-item/{id}")]
    public async Task<IActionResult> DeleteItem(int id, [FromQuery] string token)
    {
        if (!CheckToken(token)) return Unauthorized();
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound();
        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
        return Ok();
    }

    public record SaveCategoryDto(int? Id, int VenueId, string Name);

    [HttpPost("save-category")]
    public async Task<IActionResult> SaveCategory([FromBody] SaveCategoryDto dto, [FromQuery] string token)
    {
        if (!CheckToken(token)) return Unauthorized();

        if (dto.Id.HasValue && dto.Id > 0)
        {
            var cat = await _db.Categories.FindAsync(dto.Id.Value);
            if (cat == null) return NotFound();
            cat.Name = dto.Name.Trim();
        }
        else
        {
            var maxSort = await _db.Categories.Where(c => c.VenueId == dto.VenueId).MaxAsync(c => (int?)c.SortOrder) ?? 0;
            _db.Categories.Add(new Category { VenueId = dto.VenueId, Name = dto.Name.Trim(), SortOrder = maxSort + 1, IsDefault = false });
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("delete-category/{id}")]
    public async Task<IActionResult> DeleteCategory(int id, [FromQuery] string token)
    {
        if (!CheckToken(token)) return Unauthorized();
        var hasItems = await _db.Items.AnyAsync(i => i.CategoryId == id);
        if (hasItems) return BadRequest("has_items");
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();
        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("send-report")]
    public async Task<IActionResult> SendReport(int venueId, string token, string type,
    [FromServices] VenueHostedService venueHosted)
    {
        if (!CheckToken(token)) return Unauthorized();

        var botService = venueHosted.GetBotService(venueId);
        if (botService == null) return BadRequest("bot_not_found");

        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ashgabat");
        var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
        var today = DateTime.SpecifyKind(todayLocal, DateTimeKind.Utc);

        var managers = await _db.VenueUsers
            .Include(vu => vu.AppUser)
            .Where(vu => vu.VenueId == venueId && vu.Role == VenueRole.Boss)
            .ToListAsync();

        if (!managers.Any())
            managers = await _db.VenueUsers
                .Include(vu => vu.AppUser)
                .Where(vu => vu.VenueId == venueId && vu.Role == VenueRole.Operator)
                .ToListAsync();

        var venue = await _db.Venues.FirstOrDefaultAsync(v => v.Id == venueId);
        var allOrders = await _db.Orders
            .Where(o => o.VenueId == venueId)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
            .ToListAsync();
        var confirmed = allOrders.Where(o => o.Status == OrderStatus.Confirmed).ToList();
        var revenueTotal = confirmed.Sum(o => o.OrderItems.Sum(oi => oi.Item != null ? oi.Item.Price * oi.Quantity : 0));
        var revenueToday = confirmed.Where(o => o.CreatedAt >= today).Sum(o => o.OrderItems.Sum(oi => oi.Item != null ? oi.Item.Price * oi.Quantity : 0));
        var avgCheck = confirmed.Count > 0 ? revenueTotal / confirmed.Count : 0;

        string msg = type switch
        {
            "revenue" => $"💰 *Прибыль — {venue?.Name}*\n\n📅 Сегодня: *{revenueToday:0.00} м* ({allOrders.Count(o => o.CreatedAt >= today)} заказов)\n📊 Всего: *{revenueTotal:0.00} м* ({allOrders.Count} заказов)\n🧾 Средний чек: *{avgCheck:0.00} м*",
            "items" => BuildItemsReport(confirmed, venue?.Name ?? ""),
            "catalog" => $"🗂 *Каталог — {venue?.Name}*\n\n🛍 Товаров: *{await _db.Items.CountAsync(i => i.VenueId == venueId)}*\n📂 Категорий: *{await _db.Categories.CountAsync(c => c.VenueId == venueId)}*",
            "clients" => $"👥 *Клиенты — {venue?.Name}*\n\n🆕 Новых сегодня: *{await _db.VenueUsers.CountAsync(vu => vu.VenueId == venueId && vu.Role == VenueRole.Customer && vu.JoinedAt >= today)}*\n📊 Всего: *{await _db.VenueUsers.CountAsync(vu => vu.VenueId == venueId && vu.Role == VenueRole.Customer)}*",
            _ => "Неизвестный тип отчёта"
        };

        foreach (var m in managers)
            await botService.SendMessageAsync(m.AppUser.TelegramUserId, msg);

        return Ok();
    }

    private string BuildItemsReport(List<Order> confirmed, string venueName)
    {
        var items = confirmed.SelectMany(o => o.OrderItems)
            .Where(oi => oi.Item != null)
            .GroupBy(oi => oi.Item!.Name)
            .Select(g => new { name = g.Key, qty = g.Sum(x => x.Quantity), sum = g.Sum(x => x.Item!.Price * x.Quantity) })
            .OrderByDescending(x => x.sum).ToList();
        if (!items.Any()) return "📦 Подтверждённых заказов пока нет.";
        var total = items.Sum(i => i.sum);
        var lines = string.Join("\n", items.Select(i => $"• {i.name} ×{i.qty} — {i.sum:0.00} м"));
        return $"📦 *По товарам — {venueName}*\n\n{lines}\n\n💰 Итого: *{total:0.00} м*";
    }
    [HttpGet("validate-code")]
    public IActionResult ValidateCode(string code, [FromServices] ManagerAccessService accessService)
    {
        var venueId = accessService.ValidateCode(code);
        if (venueId == null) return Unauthorized();
        return Ok(new { token = _config["ManagerToken"], venueId = venueId.Value });
    }

    [HttpGet("venue-status")]
    public async Task<IActionResult> GetVenueStatus(int venueId, string token)
    {
        if (!CheckToken(token)) return Unauthorized();
        var venue = await _db.Venues.FindAsync(venueId);
        if (venue == null) return NotFound();
        return Ok(new { isOpen = venue.IsOpen, name = venue.Name });
    }

    [HttpPost("toggle-status")]
    public async Task<IActionResult> ToggleStatus(int venueId, string token)
    {
        if (!CheckToken(token)) return Unauthorized();
        var venue = await _db.Venues.FindAsync(venueId);
        if (venue == null) return NotFound();
        venue.IsOpen = !venue.IsOpen;
        await _db.SaveChangesAsync();
        return Ok(new { isOpen = venue.IsOpen });
    }
}