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

    [HttpPost("send-sms-to-number")]
    public async Task<IActionResult> SendSmsToNumber([FromForm] string phoneNumber, [FromForm] string message)
    {
        var response = await messageSenderService.SendSmsToNumberAsync(phoneNumber, message);
        if (response.StatusCode == (int)System.Net.HttpStatusCode.OK)
        {
            return Ok(response);
        }
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("send-email-to-address")]
    public async Task<IActionResult> SendEmailToAddress([FromForm] SendEmailToAddressDto request)
    {
        var response = await messageSenderService.SendEmailToAddressAsync(request);
        if (response.StatusCode == (int)System.Net.HttpStatusCode.OK)
        {
            return Ok(response);
        }
        return StatusCode((int)response.StatusCode, response);
    }
}
