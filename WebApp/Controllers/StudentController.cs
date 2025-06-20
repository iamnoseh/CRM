using Domain.DTOs.Student;
using Domain.Entities;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentController (IStudentService service) : ControllerBase
{
    [HttpGet]
    public async Task<Response<List<GetStudentDto>>> GetAllStudents() =>
        await service.GetStudents();

    [HttpGet("{id}")]
    public async Task<Response<GetStudentDto>> GetStudentById(int id ) => 
        await service.GetStudentByIdAsync(id );

    [HttpGet("studentAverage/{id}")]
    public async Task<Response<GetStudentAverageDto>> GetStudentAverageById(int studentId, int groupId) =>
        await service.GetStudentAverageAsync(studentId, groupId);
    [HttpGet("filter")]
    public async Task<PaginationResponse<List<GetStudentDto>>> 
        GetStudentsPagination([FromQuery] StudentFilter filter ) =>
        await service.GetStudentsPagination(filter );

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<Response<string>> CreateStudent([FromForm] CreateStudentDto student) => 
        await service.CreateStudentAsync(student);

    [HttpPut("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Student}")]
    public async Task<Response<string>> UpdateStudent(int id, [FromForm] UpdateStudentDto dto) =>
        await service.UpdateStudentAsync(id, dto);
        
    [HttpPut("profile/{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Student}")]
    public async Task<Response<string>> UpdateStudentProfile(int id, IFormFile photo) =>
        await service.UpdateUserProfileImageAsync(id, photo);
        
    [HttpPut("document/{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Student}")]
    public async Task<Response<string>> UpdateStudentDocument(int id, IFormFile document) =>
        await service.UpdateStudentDocumentAsync(id, document);

    [HttpDelete("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<Response<string>> DeleteStudent(int id) =>
        await service.DeleteStudentAsync(id);


    [HttpGet("details/{id}")]
    public async Task<Response<GetStudentDetailedDto>> GetStudentDetailed(int id ) =>
        await service.GetStudentDetailedAsync(id );
        
    [HttpGet("document/{studentId}")]
    [Authorize]
    public async Task<IActionResult> GetStudentDocument(int studentId)
    {
        // Debug information first
        var studentDebugResponse = await service.GetStudentByIdAsync(studentId);
        if (studentDebugResponse.StatusCode != (int)System.Net.HttpStatusCode.OK)
            return StatusCode((int)studentDebugResponse.StatusCode, studentDebugResponse);
        
        var studentDebug = studentDebugResponse.Data;
        Console.WriteLine($"Student document property before download: {studentDebug?.Document ?? "NULL"}");

        var response = await service.GetStudentDocument(studentId);
        
        if (response.StatusCode != (int)System.Net.HttpStatusCode.OK)
            return StatusCode((int)response.StatusCode, response);
            
        // Get student to determine file name and type
        var studentResponse = await service.GetStudentByIdAsync(studentId);
        if (studentResponse.StatusCode != (int)System.Net.HttpStatusCode.OK)
            return File(response.Data, "application/octet-stream", $"student_{studentId}_document.pdf");
            
        // Try to get extension from the document path
        string fileName = $"student_{studentId}_document";
        string contentType = "application/octet-stream";
        
        // Find the extension if available
        var student = studentResponse.Data;
        if (student != null && !string.IsNullOrEmpty(student.Document))
        {
            string extension = Path.GetExtension(student.Document);
            if (!string.IsNullOrEmpty(extension))
            {
                fileName += extension;
                
                // Set appropriate content type based on file extension
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
        
        // Return the document as a downloadable file
        return File(response.Data, contentType, fileName);
    }
    
    // Debugging route to check and fix document paths
    [HttpGet("debug/document/{studentId}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> DebugStudentDocument(int studentId)
    {
        try
        {
            // Get raw student data from DB 
            var student = await service.GetStudentByIdAsync(studentId);
            
            if (student.StatusCode != (int)System.Net.HttpStatusCode.OK)
                return StatusCode((int)student.StatusCode, student);
            
            // Create a debug info object
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
    [Authorize(Roles = "Admin")]
    public async Task<Response<string>> UpdateStudentPaymentStatus(int studentId , PaymentStatus status)
        => await service.UpdateStudentPaymentStatusAsync(studentId , status);
}