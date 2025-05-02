namespace Domain.DTOs.Statistics;

public class StudentPerformanceDto : BaseStatisticsDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    
    // Миёнаи баҳо аз рӯи дарсҳо
    public double AverageLessonGrade { get; set; }
    
    // Миёнаи баҳо аз рӯи имтиҳонҳои ҳафтагӣ
    public double AverageWeeklyExamGrade { get; set; }
    
    // Миёнаи баҳо аз рӯи имтиҳонҳои ниҳоӣ
    public double AverageFinalExamGrade { get; set; }
    
    // Миёнаи умумии вазндор
    public double TotalWeightedAverage { get; set; }
    
    // Фоизи иштирок (ҳозирӣ)
    public double AttendanceRate { get; set; }
    
    // Шумораи иштирок
    public int AttendanceCount { get; set; }
    
    // Шумораи ғоибӣ
    public int AbsenceCount { get; set; }
    
    // Шумораи бонусҳо
    public int TotalBonusPoints { get; set; }
    
    // Ҳафтаи ҷорӣ
    public int CurrentWeek { get; set; }
    
    // Маълумоти вазъият оид ба пардохтҳо
    public string PaymentStatus { get; set; } = string.Empty;
    
    // Санаи охирин навсозӣ
    public DateTime LastUpdated { get; set; }
}
