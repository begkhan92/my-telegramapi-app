using Microsoft.AspNetCore.Mvc;

namespace bnmini_crm.Controllers
{
    // Controllers/UploadController.cs
    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        [HttpPost("item-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0) return BadRequest("No file");
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (!allowed.Contains(ext)) return BadRequest("Invalid format");
                if (file.Length > 5 * 1024 * 1024) return BadRequest("Too large");
                var fileName = $"{Guid.NewGuid()}{ext}";
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "items");
                Console.WriteLine($"📁 Saving to: {dir}");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, fileName);
                await using var fs = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(fs);
                Console.WriteLine($"✅ Saved: {path}");
                return Ok(new { url = $"/images/items/{fileName}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Upload error: {ex}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
