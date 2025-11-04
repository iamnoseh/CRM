using Domain.DTOs.Payments;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(IPaymentService service, IReceiptService receiptService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<GetPaymentDto>>> Create([FromBody] CreatePaymentDto dto)
    {
        var res = await service.CreateAsync(dto);
        return StatusCode(res.StatusCode, res);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<GetPaymentDto>>> GetById(int id)
    {
        var res = await service.GetByIdAsync(id);
        return StatusCode(res.StatusCode, res);
    }

    [HttpGet("{id}/receipt")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<object>> GetReceipt(int id, [FromQuery] string? format)
    {
        var res = await service.GetByIdAsync(id);
        if (res.Data == null)
            return StatusCode(res.StatusCode, res);

        try
        {
            var (receiptNumber, url) = await receiptService.GenerateOrGetReceiptAsync(id, string.IsNullOrWhiteSpace(format) ? "html" : format);
            return Ok(new
            {
                statusCode = 200,
                data = new
                {
                    paymentId = res.Data.Id,
                    receiptNumber,
                    downloadUrl = url
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { statusCode = 500, message = ex.Message });
        }
    }

    [HttpPost("{id}/refund")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<bool>>> Refund(int id, [FromBody] Domain.DTOs.Payments.RefundPaymentDto dto)
    {
        var res = await service.RefundAsync(id, dto.Amount, dto.Reason);
        return StatusCode(res.StatusCode, res);
    }
}
