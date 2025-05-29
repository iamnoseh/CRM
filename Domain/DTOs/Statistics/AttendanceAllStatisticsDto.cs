namespace Domain.DTOs.Statistics;
public class AttendanceAllStatisticsDto
{
    public int TotalLessons { get; set; }
    public int PresentCount { get; set; }

    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    
    public double AttendancePercentage { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
}

public class StudentAttendanceAllStatisticsDto : AttendanceAllStatisticsDto
{
    public int StudentId { get; set; }

    /// <summary>
    /// Ному насаби хонанда
    /// </summary>
    public required string StudentName { get; set; }

    /// <summary>
    /// Рамзи гурӯҳ
    /// </summary>
    public int GroupId { get; set; }
    public required string GroupName { get; set; }
}


public class GroupAttendanceAllStatisticsDto : AttendanceAllStatisticsDto
{
    public int GroupId { get; set; }
    public required string GroupName { get; set; }
    public int TotalStudents { get; set; }
    public List<StudentAttendanceAllStatisticsDto> TopStudents { get; set; } = new();
    public List<StudentAttendanceAllStatisticsDto> LowAttendanceStudents { get; set; } = new();
}

public class CenterAttendanceAllStatisticsDto : AttendanceAllStatisticsDto
{
    public int CenterId { get; set; }
    public required string CenterName { get; set; }
    public int TotalGroups { get; set; }
    public int TotalStudents { get; set; }
    public List<GroupAttendanceAllStatisticsDto> GroupStatistics { get; set; } = new();
}
