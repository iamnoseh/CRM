// using Domain.DTOs.StudentGroup;
// using Domain.Entities;
// using Domain.Responses;
// using Infrastructure.Interfaces;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
//
// namespace WebApp.Controllers;
//
// [ApiController]
// [Route("api/[controller]")]
// public class StudentGroupController(IStudentGroupService service) : ControllerBase
// {
//     [HttpGet]
//     public async Task<Response<List<GetStudentGroupDto>>> GetAllStudentGroups(string language = "En") =>
//         await service.GetAllStudentGroupsAsync(language);
//
//     [HttpGet("{id}")]
//     public async Task<Response<GetStudentGroupDto>> GetStudentGroupById(int id, string language = "En") =>
//         await service.GetStudentGroupByIdAsync(id, language);
//         
//     [HttpGet("student/{studentId}")]
//     public async Task<Response<List<GetStudentGroupDto>>> GetStudentGroupsByStudent(int studentId, string language = "En") =>
//         await service.GetStudentGroupsByStudentAsync(studentId, language);
//         
//     [HttpGet("group/{groupId}")]
//     public async Task<Response<List<GetStudentGroupDto>>> GetStudentGroupsByGroup(int groupId, string language = "En") =>
//         await service.GetStudentGroupsByGroupAsync(groupId, language);
//         
//     [HttpPost]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> CreateStudentGroup(CreateStudentGroup request) =>
//         await service.CreateStudentGroupAsync(request);
//
//     [HttpPut("{id}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> UpdateStudentGroup(int id, UpdateStudentGroupDto request) =>
//         await service.UpdateStudentGroupAsync(id, request);
//
//     [HttpDelete("{id}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> DeleteStudentGroup(int id) => 
//         await service.DeleteStudentGroupAsync(id);
// }