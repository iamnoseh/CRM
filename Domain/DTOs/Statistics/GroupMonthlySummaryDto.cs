namespace Domain.DTOs.Statistics;

public class GroupMonthlySummaryDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    
    // Шумораи донишҷӯён дар гурӯҳ
    public int StudentsCount { get; set; }
    
    // Шумораи донишҷӯёни фаъол
    public int ActiveStudentsCount { get; set; }
    
    // Шумораи донишҷӯён бо мушкилоти пардохт
    public int StudentsWithPaymentIssuesCount { get; set; }
    
    // Даромад аз гурӯҳ дар моҳи ҷорӣ
    public decimal MonthlyRevenue { get; set; }
    
    // Миёнаи иштирок дар гурӯҳ
    public double AverageAttendanceRate { get; set; }
    
    // Миёнаи баҳоҳо дар гурӯҳ
    public double AverageGrade { get; set; }
    
    // Миёнаи баҳои имтиҳонҳо
    public double AverageExamGrade { get; set; }
    
    // Мақоми гурӯҳ
    public string Status { get; set; } = string.Empty;
}
