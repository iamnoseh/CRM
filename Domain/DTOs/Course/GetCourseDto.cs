using Domain.Enums;

namespace Domain.DTOs.Course;

public class GetCourseDto 
{
    public int Id { get; set; }
    public string? CourseName { get; set; }
    public string? Description { get; set; }
    public int DurationInMonth { get; set; }
    public decimal Price { get; set; }
    public ActiveStatus Status { get; set; }
    public string? ImagePath { get; set; }
    public int CenterId { get; set; }
    public string? CenterName { get; set; }
}