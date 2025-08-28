using Domain.DTOs.User.Employee;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(IEmployeeService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Manager")] 
    public async Task<ActionResult<PaginationResponse<List<GetEmployeeDto>>>> GetAll([FromQuery] EmployeeFilter filter)
    {
        var result = await service.GetEmployeesAsync(filter);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin,Manager")]
    public async Task<ActionResult<Response<GetEmployeeDto>>> GetById(int id)
    {
        var result = await service.GetEmployeeAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("managers/select")]
    [Authorize(Roles = "SuperAdmin,Manager")]
    public async Task<ActionResult<Response<List<ManagerSelectDto>>>> GetManagersForSelect()
    {
        var result = await service.GetManagersForSelectAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> Create([FromForm] CreateEmployeeDto dto)
    {
        var result = await service.CreateEmployeeAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut]
    [Authorize(Roles = "SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> Update([FromForm] UpdateEmployeeDto dto)
    {
        var result = await service.UpdateEmployeeAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> Delete(int id)
    {
        var result = await service.DeleteEmployeeAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("payment-status")] 
    [Authorize(Roles = "SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> UpdatePaymentStatus([FromBody] UpdateEmployeePaymentStatusDto dto)
    {
        var result = await service.UpdateEmployeePaymentStatusAsync(dto.EmployeeId, dto.Status);
        return StatusCode(result.StatusCode, result);
    }
} 