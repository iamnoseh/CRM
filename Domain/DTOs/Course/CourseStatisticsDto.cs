namespace Domain.DTOs.Course;

public class CourseStatisticsDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = null!;
    
    // Общая статистика
    public int TotalGroups { get; set; }
    public int TotalStudents { get; set; }
    public int TotalMentors { get; set; }
    
    // Статистика активности
    public int ActiveGroups { get; set; }
    public int ActiveStudents { get; set; }
    
    // Статистика успеваемости
    public double AverageGrade { get; set; }
    public Dictionary<string, double> AverageGradeByGroup { get; set; } = new Dictionary<string, double>();
    
    // Статистика посещаемости
    public double AverageAttendanceRate { get; set; }
    public Dictionary<string, double> AttendanceRateByGroup { get; set; } = new Dictionary<string, double>();
    
    // Информация о выпускниках
    public int GraduatedStudents { get; set; }
    public double GraduationRate { get; set; }
    
    // Популярность курса
    public int EnrollmentTrend { get; set; } // положительное число - рост, отрицательное - падение
    public double StudentSatisfactionRate { get; set; }
}
