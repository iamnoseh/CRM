using Domain.DTOs.Finance;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController(IExpenseService expenseService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Response<GetExpenseDto>>> Create([FromBody] CreateExpenseDto dto)
    {
        var response = await expenseService.CreateAsync(dto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Response<GetExpenseDto>>> Update(int id, [FromBody] UpdateExpenseDto dto)
    {
        var response = await expenseService.UpdateAsync(id, dto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Response<bool>>> Delete(int id)
    {
        var response = await expenseService.DeleteAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Response<GetExpenseDto>>> GetById(int id)
    {
        var response = await expenseService.GetByIdAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Response<List<GetExpenseDto>>>> Get([FromQuery] ExpenseFilter filter)
    {
        var response = await expenseService.GetAsync(filter);
        return StatusCode(response.StatusCode, response);
    }
}