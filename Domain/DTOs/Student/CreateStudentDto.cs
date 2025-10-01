using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.Student;

public class CreateStudentDto
{
    public string FullName { get; set; } = string.Empty;
    public required string Email { get; set; }
    public string Address { get; set; } = string.Empty;
    public required string PhoneNumber { get; set; }
    public DateTime Birthday { get; set; }
    public Gender Gender { get; set; }
    public IFormFile? ProfilePhoto { get; set; }
    public IFormFile? DocumentFile { get; set; }
}