namespace Domain.DTOs.Statistics;

public class GroupPerformanceDto : BaseStatisticsDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    
    // Миёнаи ҳамаи баҳоҳои гурӯҳ
    public double AverageGroupGrade { get; set; }
    
    // Миёнаи баҳоҳои имтиҳонҳои ҳафтагӣ
    public double AverageWeeklyExamGrade { get; set; }
    
    // Миёнаи баҳоҳои имтиҳонҳои ниҳоӣ
    public double AverageFinalExamGrade { get; set; }
    
    // Миёнаи ҳузур дар гурӯҳ (бо фоиз)
    public double AverageAttendanceRate { get; set; }
    
    // Ҳафтаи ҷорӣ
    public int CurrentWeek { get; set; }
    
    // Шумораи донишҷӯён
    public int StudentsCount { get; set; }
    
    // Шумораи донишҷӯёни фаъол
    public int ActiveStudentsCount { get; set; }
    
    // Шумораи донишҷӯёни бо пардохти имрӯза
    public int PaidStudentsCount { get; set; }
    
    // Шумораи донишҷӯёне, ки қарздоранд
    public int UnpaidStudentsCount { get; set; }
    
    // Рӯйхати натиҷаҳои донишҷӯён
    public List<StudentPerformanceDto> StudentsPerformance { get; set; } = new();
    
    // Санаи охирин навсозӣ
    public DateTime LastUpdated { get; set; }
}
