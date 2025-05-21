using Domain.DTOs.Exam;
using Domain.DTOs.Grade;

namespace Domain.DTOs.Student;

public class StudentStatisticsDto
{
    public GetStudentDto Student { get; set; }
    public double AverageGrade { get; set; }
    public List<GetExamDto> RecentExams { get; set; } = new();
    public List<GetLessonGradeDto> RecentGrades { get; set; } = new();
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int CurrentWeek { get; set; }
} 