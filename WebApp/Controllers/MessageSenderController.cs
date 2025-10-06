using Domain.DTOs.MessageSender;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class MessageSenderController(IMessageSenderService messageSenderService) : ControllerBase
{
    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessage([FromForm] SendMessageDto sendMessageDto)
    {
        var response = await messageSenderService.SendMessageAsync(sendMessageDto);
        if (response.StatusCode == (int)System.Net.HttpStatusCode.OK)
        {
            return Ok(response);
        }
        return StatusCode((int)response.StatusCode, response);
    }
}
