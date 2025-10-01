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
public class LeadController(ILeadService leadService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<ActionResult<Response<string>>> CreateLead([FromBody] CreateLeadDto request)
    {
        var result = await leadService.CreateLead(request);
        return StatusCode((int)result.StatusCode, result);
    }

   
    [HttpPut]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<ActionResult<Response<string>>> UpdateLead([FromBody] UpdateLeadDto request)
    {
        var result = await leadService.UpdateLead(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<ActionResult<Response<string>>> DeleteLead(int id)
    {
        var result = await leadService.DeleteLead(id);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<ActionResult<PaginationResponse<List<GetLeadDto>>>> GetLeads([FromQuery] LeadFilter filter)
    {
        var result = await leadService.GetLeads(filter);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpGet("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<ActionResult<Response<GetLeadDto>>> GetLead(int id)
    {
        var result = await leadService.GetLead(id);
        return StatusCode((int)result.StatusCode, result);
    }
    
}
