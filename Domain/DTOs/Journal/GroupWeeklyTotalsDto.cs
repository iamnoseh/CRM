namespace Domain.DTOs.Journal;

public class GroupWeeklyTotalsDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<WeekTotalsDto> Weeks { get; set; } = new();
    public List<StudentAggregateDto> StudentAggregates { get; set; } = new();
}

public class WeekTotalsDto
{
    public int WeekNumber { get; set; }
    public DateTimeOffset WeekStartDate { get; set; }
    public DateTimeOffset WeekEndDate { get; set; }
    public List<StudentWeekPointsDto> Students { get; set; } = new();
}

public class StudentWeekPointsDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal TotalPoints { get; set; }
    public bool IsActive { get; set; }
}

public class StudentAggregateDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal TotalPointsAllWeeks { get; set; }
    public double AveragePointsPerWeek { get; set; }
    public bool IsActive { get; set; }
}


