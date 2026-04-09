using Microsoft.AspNetCore.Mvc;
using bnmini_crm.Data;
using bnmini_crm.Models;
using Microsoft.EntityFrameworkCore;

namespace bnmini_crm.Controllers;

[ApiController]
[Route("api/shop")]
public class ShopApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public ShopApiController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet("init")]
    public async Task<IActionResult> Init(int venueId, int userId)
    {
        var items = await _db.Items
            .Where(i => i.VenueId == venueId)
            .Include(i => i.Images)
            .ToListAsync();

        var categories = await _db.Categories
            .Where(c => c.VenueId == venueId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        string? phone = null;
        string? firstName = null;
        List<string> addresses = new();

        if (userId > 0)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                firstName = user.FirstName;
                phone = user.Phone;
                addresses = await _db.DeliveryAddresses
                    .Where(a => a.AppUserId == userId)
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => a.Label)
                    .ToListAsync();
            }
        }

        return Ok(new
        {
            items = items.Select(i => new
            {
                i.Id,
                i.Name,
                i.Description,
                i.Price,
                i.ImageUrl,
                CategoryId = i.CategoryId ?? 0,
                Images = i.Images.OrderBy(img => img.SortOrder)
                    .Select(img => new { img.Url }).ToList()
            }),
            categories = categories.Select(c => new { c.Id, c.Name, c.IsDefault }),
            firstName = firstName ?? "",
            phone = phone ?? "",
            addresses,
            defaultCategoryId = categories.FirstOrDefault(c => c.IsDefault)?.Id ?? 0,
            currency = _config["ShopConfig:Currency"] ?? "TMT",
            deliveryFee = decimal.Parse(_config["ShopConfig:DeliveryFee"] ?? "0", System.Globalization.CultureInfo.InvariantCulture),
            language = _config["ShopConfig:Language"] ?? "tk",
            isOpen = (await _db.Venues.FindAsync(venueId))?.IsOpen ?? true
        });
    }
    [HttpGet("user-by-telegram")]
    public async Task<IActionResult> GetUserByTelegram(long telegramId)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.TelegramUserId == telegramId);
        if (user == null) return NotFound();
        return Ok(new { id = user.Id, user.FirstName });
    }
}