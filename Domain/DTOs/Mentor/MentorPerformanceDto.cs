namespace Domain.DTOs.Mentor;

public class MentorPerformanceDto
{
    public int MentorId { get; set; }
    public string MentorName { get; set; } = null!;
    
    // Общая информация
    public int TotalGroups { get; set; }
    public int TotalStudents { get; set; }
    public int TotalLessons { get; set; }
    
    // Статистика посещаемости на занятиях
    public double AverageAttendanceRate { get; set; }
    public Dictionary<string, double> AttendanceRateByGroup { get; set; } = new Dictionary<string, double>();
    
    // Успеваемость студентов
    public double AverageStudentGrade { get; set; }
    public Dictionary<string, double> AverageGradeByGroup { get; set; } = new Dictionary<string, double>();
    
    // Активность
    public int LessonsLastMonth { get; set; }
    public DateTime LastActive { get; set; }
}
