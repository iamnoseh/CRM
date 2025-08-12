using Domain.Enums;

namespace Domain.DTOs.Journal;

public class GetJournalEntryDto
{
    public int Id { get; set; }
    public int DayOfWeek { get; set; }
    public int LessonNumber { get; set; } 
    public LessonType LessonType { get; set; } = LessonType.Regular;
    public decimal Grade { get; set; } = 0;
    public decimal BonusPoints { get; set; } = 0;
    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Absent;
    public string? Comment { get; set; }
    public CommentCategory CommentCategory { get; set; } = CommentCategory.General;
    public DateTime EntryDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
}