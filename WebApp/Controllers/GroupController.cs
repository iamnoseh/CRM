// using Domain.DTOs.Group;
// using Domain.Entities;
// using Domain.Filters;
// using Domain.Responses;
// using Infrastructure.Interfaces;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
//
// namespace WebApp.Controllers;
//
// [ApiController]
// [Route("api/[controller]")]
// public class GroupController(IGroupService service) : ControllerBase
// {
//     [HttpGet("filter")]
//     public async Task<PaginationResponse<List<GetGroupDto>>> GetGroupsPagination([FromQuery]GroupFilter filter, string language = "En") =>
//         await service.GetGroupPaginated(filter, language);
//
//     [HttpGet]
//     public async Task<Response<List<GetGroupDto>>> GetAllGroups(string language = "En") =>
//         await service.GetGroups(language);
//
//     [HttpGet("{id}")]
//     public async Task<Response<GetGroupDto>> GetGroupById(int id, string language = "En") =>
//         await service.GetGroupByIdAsync(id, language);
//     
//     [HttpPost]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> CreateGroup([FromForm] CreateGroupDto dto) => 
//         await service.CreateGroupAsync(dto);
//
//     [HttpPut("{id}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> UpdateGroup(int id, [FromForm] UpdateGroupDto dto) =>
//         await service.UpdateGroupAsync(id,dto);
//     
//     [HttpDelete("{id}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> DeleteGroup(int id) => 
//         await service.DeleteGroupAsync(id);
//
//     [HttpGet("attendance-statistics/{groupId}")]
//     public async Task<Response<GroupAttendanceStatisticsDto>> GetGroupAttendanceStatistics(int groupId) =>
//         await service.GetGroupAttendanceStatisticsAsync(groupId);
// }