
namespace Domain.DTOs.Statistics;

public class AttendanceStatisticsDto
{
    public int TotalLessons { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public double AttendancePercentage { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
}
public class StudentAttendanceStatisticsDto : AttendanceStatisticsDto
{
    public int StudentId { get; set; }
    public required string StudentName { get; set; }
    public int GroupId { get; set; }
    public required string GroupName { get; set; }
}


public class GroupAttendanceStatisticsDto : AttendanceStatisticsDto
{
    public int GroupId { get; set; }
    public required string GroupName { get; set; }
    public int TotalStudents { get; set; }
    public List<StudentAttendanceStatisticsDto> TopStudents { get; set; } = new();
    public List<StudentAttendanceStatisticsDto> LowAttendanceStudents { get; set; } = new();
}

public class CenterAttendanceStatisticsDto : AttendanceStatisticsDto
{

    public int CenterId { get; set; }
    public required string CenterName { get; set; }
    public int TotalGroups { get; set; }
    public List<GroupAttendanceStatisticsDto> GroupStatistics { get; set; } = new();
}
