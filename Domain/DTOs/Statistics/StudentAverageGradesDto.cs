namespace Domain.DTOs.Statistics;

public class StudentAverageGradesDto : BaseStatisticsDto
{
    public int StudentId { get; set; }
    public string StudentNameTj { get; set; } = string.Empty;
    public string StudentNameRu { get; set; } = string.Empty;
    public string StudentNameEn { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    
    // Миёнаи баҳо аз рӯи дарсҳо
    public double AverageLessonGrade { get; set; }
    
    // Миёнаи баҳо аз рӯи имтиҳонҳои ҳафтагӣ
    public double AverageWeeklyExamGrade { get; set; }
    
    // Миёнаи баҳо аз рӯи имтиҳонҳои ниҳоӣ
    public double AverageFinalExamGrade { get; set; }
    
    // Миёнаи умумӣ бо назардошти вазн (weight)
    public double TotalWeightedAverage { get; set; }
    
    // Миёнаи умумӣ бе назардошти вазн
    public double TotalAverage { get; set; }
    
    // Баҳои баландтарин
    public int? HighestGrade { get; set; }
    
    // Баҳои пасттарин
    public int? LowestGrade { get; set; }
    
    // Шумораи умумии баҳоҳо
    public int TotalGradesCount { get; set; }
    
    // Шумораи бонусҳо
    public int TotalBonusPoints { get; set; }
}
