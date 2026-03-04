using bnmini_crm.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace bnmini_crm.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelegramController : ControllerBase
    {
        private readonly TelegramBotService _botService;

        public TelegramController(TelegramBotService botService)
        {
            _botService = botService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            await _botService.HandleUpdateAsync(update);
            return Ok();
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromQuery] long chatId, [FromQuery] string text)
        {
            await _botService.SendMessageAsync(chatId, text);
            return Ok(new { status = "Message sent" });
        }


    }
}
