using Domain.Entities;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupActivationController(IGroupActivationService service) : ControllerBase
{
    [HttpPost("activate/{groupId}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<Response<string>> ActivateGroup(int groupId)
    {
        return await service.ActivateGroupAsync(groupId);
    }
}
