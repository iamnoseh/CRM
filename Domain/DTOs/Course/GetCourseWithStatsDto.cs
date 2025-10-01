namespace Domain.DTOs.Course;

public class GetCourseWithStatsDto
{
    public int Id { get; set; }
    public string CourseName { get; set; }
    public string? Image { get; set; }
    public decimal Price { get; set; }
    public int GroupCount { get; set; }
    public int StudentCount { get; set; }
} 