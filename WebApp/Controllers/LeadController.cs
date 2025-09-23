using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.DTOs.Lead;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeadController : ControllerBase
{
    private readonly ILeadService _leadService;

    public LeadController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<ActionResult<Response<string>>> CreateLead([FromBody] CreateLeadDto request)
    {
        var result = await _leadService.CreateLead(request);
        return StatusCode((int)result.StatusCode, result);
    }

   
    [HttpPut]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<ActionResult<Response<string>>> UpdateLead([FromBody] UpdateLeadDto request)
    {
        var result = await _leadService.UpdateLead(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<ActionResult<Response<string>>> DeleteLead(int id)
    {
        var result = await _leadService.DeleteLead(id);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<ActionResult<PaginationResponse<List<GetLeadDto>>>> GetLeads([FromQuery] LeadFilter filter)
    {
        var result = await _leadService.GetLeads(filter);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpGet("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<ActionResult<Response<GetLeadDto>>> GetLead(int id)
    {
        var result = await _leadService.GetLead(id);
        return StatusCode((int)result.StatusCode, result);
    }
    
}
