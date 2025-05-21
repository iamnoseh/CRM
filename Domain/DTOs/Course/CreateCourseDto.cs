using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.Course;

public class CreateCourseDto
{
    public string CourseName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationInMonth { get; set; }
    public decimal Price { get; set; }
    public ActiveStatus Status { get; set; }
    public IFormFile? ImageFile { get; set; }
    public int CenterId { get; set; }
}