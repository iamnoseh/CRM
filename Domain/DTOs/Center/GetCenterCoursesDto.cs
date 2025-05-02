using Domain.DTOs.Course;

namespace Domain.DTOs.Center;

public class GetCenterCoursesDto
{
    public int CenterId { get; set; }
    public string CenterName { get; set; } = null!;
    public List<GetCourseDto> Courses { get; set; } = new List<GetCourseDto>();
    public int TotalCourses { get; set; }
    public int ActiveCourses { get; set; }
}
