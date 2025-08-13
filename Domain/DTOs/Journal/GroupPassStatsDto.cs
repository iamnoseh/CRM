namespace Domain.DTOs.Journal;

public class GroupPassStatsDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int PassedCount { get; set; }
    public decimal Threshold { get; set; }
}


