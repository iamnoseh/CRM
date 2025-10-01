using Domain.DTOs.Student;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Infrastructure.Services.ExportToExel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentController (IStudentService service) : ControllerBase
{
    [HttpGet("select-students")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> GetStudentForSelect([FromQuery] StudentFilterForSelect filter)
    {
        var result = await service.GetStudentForSelect(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<Response<GetStudentDto>> GetStudentById(int id ) => 
        await service.GetStudentByIdAsync(id );
    
    [HttpGet("filter")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<PaginationResponse<List<GetStudentDto>>> 
        GetStudentsPagination([FromQuery] StudentFilter filter ) =>
        await service.GetStudentsPagination(filter );

    [HttpGet("simple")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<PaginationResponse<List<GetSimpleDto>>> 
        GetSimpleStudents([FromQuery] StudentFilter filter) =>
        await service.GetSimpleStudents(filter);

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<Response<string>> CreateStudent([FromForm] CreateStudentDto student) => 
        await service.CreateStudentAsync(student);

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<Response<string>> UpdateStudent(int id, [FromForm] UpdateStudentDto dto) =>
        await service.UpdateStudentAsync(id, dto);
        
    [HttpPut("profile/{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Student")]
    public async Task<Response<string>> UpdateStudentProfile(int id, IFormFile photo) =>
        await service.UpdateUserProfileImageAsync(id, photo);
        
    [HttpPut("document/{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<Response<string>> UpdateStudentDocument(int id, IFormFile document) =>
        await service.UpdateStudentDocumentAsync(id, document);

    [HttpDelete("{id}")][Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<Response<string>> DeleteStudent(int id) =>
        await service.DeleteStudentAsync(id);
    
        
    [HttpGet("document/{studentId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<IActionResult> GetStudentDocument(int studentId)
    {
        var studentDebugResponse = await service.GetStudentByIdAsync(studentId);
        if (studentDebugResponse.StatusCode != (int)System.Net.HttpStatusCode.OK)
            return StatusCode((int)studentDebugResponse.StatusCode, studentDebugResponse);
        
        var studentDebug = studentDebugResponse.Data;
        Console.WriteLine($"Student document property before download: {studentDebug?.Document ?? "NULL"}");

        var response = await service.GetStudentDocument(studentId);
        
        if (response.StatusCode != (int)System.Net.HttpStatusCode.OK)
            return StatusCode((int)response.StatusCode, response);
        var studentResponse = await service.GetStudentByIdAsync(studentId);
        if (studentResponse.StatusCode != (int)System.Net.HttpStatusCode.OK)
            return File(response.Data, "application/octet-stream", $"student_{studentId}_document.pdf");
        string fileName = $"student_{studentId}_document";
        string contentType = "application/octet-stream";
        var student = studentResponse.Data;
        if (student != null && !string.IsNullOrEmpty(student.Document))
        {
            string extension = Path.GetExtension(student.Document);
            if (!string.IsNullOrEmpty(extension))
            {
                fileName += extension;
                contentType = extension.ToLowerInvariant() switch
                {
                    ".pdf" => "application/pdf",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    _ => "application/octet-stream"
                };
            }
        }
        return File(response.Data, contentType, fileName);
    }
    

    [HttpGet("debug/document/{studentId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> DebugStudentDocument(int studentId)
    {
        try
        {
            var student = await service.GetStudentByIdAsync(studentId);
            
            if (student.StatusCode != (int)System.Net.HttpStatusCode.OK)
                return StatusCode((int)student.StatusCode, student);
            var debugInfo = new
            {
                StudentId = studentId,
                DocumentPath = student.Data.Document,
                HasDocumentProperty = student.Data.Document != null,
                UploadPath = "Check server configuration",
                FileSystemCheck = "Not performed",
                Suggestion = "If Document property is null, you need to upload a document"
            };
            
            return Ok(debugInfo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }

    [HttpPut("payment-status")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<Response<string>> UpdateStudentPaymentStatus([FromBody] UpdateStudentPaymentStatusDto dto)
        => await service.UpdateStudentPaymentStatusAsync(dto);
    
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
}