using Domain.DTOs.Discounts;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscountsController(IDiscountService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> Assign([FromBody] CreateStudentGroupDiscountDto dto)
    {
        var res = await service.AssignDiscountAsync(dto);
        return StatusCode(res.StatusCode, res);
    }

    [HttpPut]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> Update([FromBody] UpdateStudentGroupDiscountDto dto)
    {
        var res = await service.UpdateDiscountAsync(dto);
        return StatusCode(res.StatusCode, res);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> Remove(int id)
    {
        var res = await service.RemoveDiscountAsync(id);
        return StatusCode(res.StatusCode, res);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<Response<GetStudentGroupDiscountDto>>> Get(int id)
    {
        var res = await service.GetDiscountByIdAsync(id);
        return StatusCode(res.StatusCode, res);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<Response<List<GetStudentGroupDiscountDto>>>> GetByStudentGroup([FromQuery] int studentId, [FromQuery] int groupId)
    {
        var res = await service.GetDiscountsByStudentGroupAsync(studentId, groupId);
        return StatusCode(res.StatusCode, res);
    }

    [HttpGet("preview")]
    [Authorize]
    public async Task<ActionResult<Response<DiscountPreviewDto>>> Preview([FromQuery] int studentId, [FromQuery] int groupId, [FromQuery] int month, [FromQuery] int year)
    {
        var res = await service.PreviewAsync(studentId, groupId, month, year);
        return StatusCode(res.StatusCode, res);
    }
}
