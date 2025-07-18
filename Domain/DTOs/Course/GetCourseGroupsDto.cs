using Domain.DTOs.Group;

namespace Domain.DTOs.Course;

public class GetCourseGroupsDto
{
    public List<GetGroupDto> Groups { get; set; }
    public int Count { get; set; }
}