namespace Domain.DTOs.Statistics;

public class StudentWeeklyPerformanceDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int WeekIndex { get; set; }
    
    // Миёнаи баҳоҳои ҳафта
    public double WeeklyAverageGrade { get; set; }
    
    // Баҳои имтиҳони ҳафтагӣ, агар бошад
    public int? WeeklyExamGrade { get; set; }
    
    // Бонусҳои имтиҳони ҳафтагӣ
    public int? WeeklyExamBonusPoints { get; set; }
    
    // Фоизи ҳузури ҳафта
    public double WeeklyAttendanceRate { get; set; }
    
    // Шумораи ҳозирӣ дар ҳафта
    public int WeeklyPresentCount { get; set; }
    
    // Шумораи ғоибӣ дар ҳафта
    public int WeeklyAbsentCount { get; set; }
    
    // Шумораи деромад дар ҳафта
    public int WeeklyLateCount { get; set; }
    
    // Бонусҳои ҳафта
    public int WeeklyBonusPoints { get; set; }
    
    // Тафсирҳои ҳафта (тафсирҳои муҳим)
    public List<string> WeeklyComments { get; set; } = new();
}
