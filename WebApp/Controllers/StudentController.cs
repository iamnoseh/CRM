using Domain.DTOs.Student;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Infrastructure.Services.ExportToExel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.DTOs.Payments;
using MediatR;
using Infrastructure.Features.Students.Queries.GetStudentById;
using Infrastructure.Features.Students.Commands.CreateStudent;
using Infrastructure.Features.Students.Commands.UpdateStudent;
using Infrastructure.Features.Students.Commands.DeleteStudent;
using Infrastructure.Features.Students.Commands.UpdateUserProfileImage;
using Infrastructure.Features.Students.Commands.UpdateStudentDocument;
using Infrastructure.Features.Students.Commands.UpdateStudentPaymentStatus;
using Infrastructure.Features.Students.Queries.GetStudentForSelect;
using Infrastructure.Features.Students.Queries.GetStudentDocument;
using Infrastructure.Features.Students.Queries.GetSimpleStudents;
using Infrastructure.Features.Students.Queries.GetStudentGroupsOverview;
using Infrastructure.Features.Students.Queries.GetStudentPayments;
using Infrastructure.Features.Students.Queries.GetStudentsPagination;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentController(IMediator mediator) : ControllerBase
{
    [HttpGet("select-students")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor")]
    public async Task<IActionResult> GetStudentForSelect([FromQuery] StudentFilterForSelect filter)
    {
        var result = await mediator.Send(new GetStudentForSelectQuery(filter));
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<IActionResult> GetStudentById(int id)
    {
        var response = await mediator.Send(new GetStudentByIdQuery(id));
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> CreateStudent([FromForm] CreateStudentDto createStudentDto)
    {
        var command = new CreateStudentCommand(createStudentDto);
        var response = await mediator.Send(command);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> UpdateStudent(int id, [FromForm] UpdateStudentDto updateStudentDto)
    {
        var command = new UpdateStudentCommand(id, updateStudentDto);
        var response = await mediator.Send(command);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> DeleteStudent(int id)
    {
        var command = new DeleteStudentCommand(id);
        var response = await mediator.Send(command);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("profile-image/{studentId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Student")]
    public async Task<IActionResult> UpdateUserProfileImage(int studentId, IFormFile profileImage)
    {
        var command = new UpdateUserProfileImageCommand(studentId, profileImage);
        var response = await mediator.Send(command);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("document/{studentId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> UpdateStudentDocument(int studentId, IFormFile documentFile)
    {
        var command = new UpdateStudentDocumentCommand(studentId, documentFile);
        var response = await mediator.Send(command);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("document/{studentId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> GetStudentDocument(int studentId)
    {
        var response = await mediator.Send(new GetStudentDocumentQuery(studentId));
        if (response.StatusCode != (int)System.Net.HttpStatusCode.OK)
            return StatusCode(response.StatusCode, response.Message);

        string fileName = $"student_{studentId}_document";
        string contentType = "application/octet-stream";
        
        return File(response.Data, contentType, fileName);
    }

    [HttpPut("payment-status")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> UpdateStudentPaymentStatus([FromBody] UpdateStudentPaymentStatusDto dto)
    {
        var command = new UpdateStudentPaymentStatusCommand(dto);
        var response = await mediator.Send(command);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("{studentId}/groups-overview")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<ActionResult<Response<List<StudentGroupOverviewDto>>>> GetStudentGroupsOverview(int studentId)
    {
        var res = await mediator.Send(new GetStudentGroupsOverviewQuery(studentId));
        return StatusCode(res.StatusCode, res);
    }
    
    [HttpGet("export/analytics")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> ExportStudentAnalytics([FromServices] IStudentAnalyticsExportService exportService, [FromQuery] int? month, [FromQuery] int? year)
    {
        if ((month.HasValue && !year.HasValue) || (!month.HasValue && year.HasValue))
            return BadRequest("Both month and year must be provided together, or neither.");

        var bytes = await exportService.ExportStudentAnalyticsToExcelAsync(month, year);
        var scope = month.HasValue ? $"{year:D4}-{month:D2}" : "all";
        var fileName = $"student_analytics_{scope}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("{studentId}/payments")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Student")]
    public async Task<PaginationResponse<List<GetPaymentDto>>> GetStudentPayments(
        int studentId,
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
        => await mediator.Send(new GetStudentPaymentsQuery(studentId, month, year, pageNumber, pageSize));
    
    [HttpGet("pagination")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor")]
    public async Task<PaginationResponse<List<GetStudentDto>>> GetStudentsPagination([FromQuery] StudentFilter filter)
        => await mediator.Send(new GetStudentsPaginationQuery(filter));

    [HttpGet("simple")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor")]
    public async Task<PaginationResponse<List<GetSimpleDto>>> GetSimpleStudents([FromQuery] StudentFilter filter)
        => await mediator.Send(new GetSimpleStudentsQuery(filter));
}