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

public class StudentProgress
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<GetJournalEntryDto> StudentEntries { get; set; } = new();
}