using Domain.DTOs.Student;

namespace Domain.DTOs.Center;

public class GetCenterStudentsDto
{
    public int CenterId { get; set; }
    public string CenterName { get; set; } = null!;
    public List<GetStudentDto> Students { get; set; } = new List<GetStudentDto>();
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
}
