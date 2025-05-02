namespace Domain.Entities;

public class StudentPerformance : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; }
    
    // Баҳои миёна аз рӯи дарсҳо
    public double AverageLessonGrade { get; set; }
    
    // Баҳои миёна аз рӯи имтиҳонҳои ҳафтагӣ
    public double AverageWeeklyExamGrade { get; set; }
    
    // Баҳои миёна аз рӯи имтиҳонҳои ниҳоӣ
    public double AverageFinalExamGrade { get; set; }
    
    
    // Ҳузур ба ҳисоби фоиз
    public double AttendanceRate { get; set; }
    
    
    // Ҳафтаи ҷорӣ барои ин ҳисобот
    public int CurrentWeek { get; set; }
    
    // Сана ва вақти навсозии охирин
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}
