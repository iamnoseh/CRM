using Domain.DTOs.Payments;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(IPaymentService service) : ControllerBase
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
}
