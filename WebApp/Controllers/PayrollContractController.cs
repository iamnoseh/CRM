using Domain.DTOs.Payroll;
using Domain.Filters;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin,Manager")]
public class PayrollContractController(IPayrollContractService service) : ControllerBase
{
    #region POST api/payrollcontract

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePayrollContractDto dto)
    {
        var result = await service.CreateAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region GET api/payrollcontract

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllAsync();
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region GET api/payrollcontract/paginated

    [HttpGet("paginated")]
    public async Task<IActionResult> GetPaginated([FromQuery] PayrollContractFilter filter)
    {
        var result = await service.GetPaginatedAsync(filter);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region GET api/payrollcontract/{id}

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region GET api/payrollcontract/mentor/{mentorId}

    [HttpGet("mentor/{mentorId}")]
    public async Task<IActionResult> GetByMentor(int mentorId)
    {
        var result = await service.GetActiveByMentorAsync(mentorId);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region GET api/payrollcontract/employee/{employeeUserId}

    [HttpGet("employee/{employeeUserId}")]
    public async Task<IActionResult> GetByEmployee(int employeeUserId)
    {
        var result = await service.GetActiveByEmployeeAsync(employeeUserId);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region PUT api/payrollcontract/{id}

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePayrollContractDto dto)
    {
        var result = await service.UpdateAsync(id, dto);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region DELETE api/payrollcontract/{id}

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await service.DeactivateAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    #endregion
}
