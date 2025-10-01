namespace Domain.DTOs.Center;

public class CenterStatisticsDto
{
    public int CenterId { get; set; }
    public string CenterName { get; set; } = null!;
    
    // Общая статистика
    public int TotalStudents { get; set; }
    public int TotalGroups { get; set; }
    public int TotalMentors { get; set; }
    public int TotalCourses { get; set; }
    
    // Статистика по активности
    public int ActiveStudents { get; set; }
    public int ActiveCourses { get; set; }
    public int ActiveMentors { get; set; }
    
    // Статистика посещаемости
    public double AverageAttendanceRate { get; set; }
    public Dictionary<string, double> AttendanceByGroup { get; set; } = new Dictionary<string, double>();
    
    // Статистика успеваемости
    public double AverageGrade { get; set; }
    public Dictionary<string, double> AverageGradeByGroup { get; set; } = new Dictionary<string, double>();
    
    // Статистика по неделям
    public Dictionary<int, double> WeeklyAttendanceRates { get; set; } = new Dictionary<int, double>();
    public Dictionary<int, double> WeeklyAverageGrades { get; set; } = new Dictionary<int, double>();
    
    // Статистика оплат
    public int PaidStudents { get; set; }
    public int UnpaidStudents { get; set; }
    public double PaymentRate { get; set; }
}
