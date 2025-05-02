using Domain.DTOs.Student;

namespace Domain.DTOs.Course;

public class UpdateCourseDto : CreateCourseDto
{
    public int Id { get; set; }
}