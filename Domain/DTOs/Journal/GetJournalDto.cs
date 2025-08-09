using Domain.Enums;

namespace Domain.DTOs.Journal;

public class GetJournalDto
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public int WeekNumber { get; set; }
    public DateTimeOffset WeekStartDate { get; set; }
    public DateTimeOffset WeekEndDate { get; set; }
    public List<StudentProgress> Progresses { get; set; } = new();
}

public class GetJournalEntryDto
{
    public int Id { get; set; }
    public int DayOfWeek { get; set; } // 1..7
    public int LessonNumber { get; set; } // 1..6
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

public class StudentProgress
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<GetJournalEntryDto> StudentEntries { get; set; } = new();
}