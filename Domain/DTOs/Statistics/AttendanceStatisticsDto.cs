
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

// ДТО-ҳои нав барои талаботи шумо
public class DailyAttendanceSummaryDto
{
    public DateTime Date { get; set; }
    public int StudentsWithPaidLessons { get; set; } // Донишҷӯёне ки вақти дарсиашон шудааст
    public int PresentStudents { get; set; } // Донишҷӯёне ки ҳозиранд
    public int AbsentStudents { get; set; } // Донишҷӯёне ки ғоибанд
    public int LateStudents { get; set; } // Донишҷӯёне ки дер омадаанд
    public double AttendanceRate { get; set; } // Фоизи иштирок
}

public class AbsentStudentDto
{
    public int StudentId { get; set; }
    public required string FullName { get; set; }
    public required string PhoneNumber { get; set; }
    public int GroupId { get; set; }
    public required string GroupName { get; set; }
    public DateTime LastAttendanceDate { get; set; }
    public int ConsecutiveAbsentDays { get; set; }
}

public class MonthlyAttendanceStatisticsDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public List<DailyAttendanceSummaryDto> DailySummaries { get; set; } = new();
    public List<AbsentStudentDto> AbsentStudents { get; set; } = new();
    public double MonthlyAverageAttendance { get; set; }
    public int TotalStudentsWithPaidLessons { get; set; }
    public int TotalPresentDays { get; set; }
    public int TotalAbsentDays { get; set; }
}
