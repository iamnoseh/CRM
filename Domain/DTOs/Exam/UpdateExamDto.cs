namespace Domain.DTOs.Exam;

public class UpdateExamDto
{
    public int ExamId { get; set; }
    public int? Value { get; set; }
    public int? BonusPoints { get; set; }
    public string? Comment { get; set; }
    public int? WeekIndex { get; set; }
    public DateTime? ExamDate { get; set; }
} 