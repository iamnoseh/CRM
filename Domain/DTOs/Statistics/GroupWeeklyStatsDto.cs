namespace Domain.DTOs.Statistics;

public class GroupWeeklyStatsDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int WeekIndex { get; set; }
    
    // Миёнаи баҳоҳои ҳафта
    public double WeeklyAverageGrade { get; set; }
    
    // Миёнаи ҳузури ҳафта (бо фоиз)
    public double WeeklyAttendanceRate { get; set; }
    
    // Миёнаи баҳои имтиҳони ҳафтагӣ, агар бошад
    public double? WeeklyExamAverageGrade { get; set; }
    
    // Донишҷӯ бо баҳои баландтарин
    public StudentBriefDto TopStudent { get; set; } = new();
    
    // Донишҷӯ бо баҳои пасттарин
    public StudentBriefDto LowStudent { get; set; } = new();
    
    // Донишҷӯ бо ҳузури пасттарин
    public StudentBriefDto LowestAttendanceStudent { get; set; } = new();
    
    // Шумораи умумии дарсҳо дар ҳафта
    public int TotalLessonsInWeek { get; set; }
    
    // Оё имтиҳони ҳафтагӣ вуҷуд дошт
    public bool HasWeeklyExam { get; set; }
    
    // Рӯйхати натиҷаҳои донишҷӯён дар ин ҳафта
    public List<StudentWeeklyPerformanceDto> StudentPerformances { get; set; } = new();
}

public class StudentBriefDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public double Value { get; set; } // Баҳо ё фоизи ҳузур
}
