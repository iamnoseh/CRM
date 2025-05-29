using Domain.Enums;

namespace Domain.DTOs.Statistics;

/// <summary>
/// Модели асосӣ барои маълумоти оморӣ дар бораи давомот
/// </summary>
public class AttendanceAllStatisticsDto
{
    /// <summary>
    /// Миқдори умумии дарсҳо
    /// </summary>
    public int TotalLessons { get; set; }

    /// <summary>
    /// Миқдори дарсҳое, ки хонанда иштирок кардааст
    /// </summary>
    public int PresentCount { get; set; }

    /// <summary>
    /// Миқдори дарсҳое, ки хонанда ғоиб будааст
    /// </summary>
    public int AbsentCount { get; set; }

    /// <summary>
    /// Миқдори дарсҳое, ки хонанда дер омадааст
    /// </summary>
    public int LateCount { get; set; }

    /// <summary>
    /// Фоизи иштироки хонанда дар дарсҳо
    /// </summary>
    public double AttendancePercentage { get; set; }

    /// <summary>
    /// Санаи оғози давра
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Санаи анҷоми давра
    /// </summary>
    public DateTimeOffset EndDate { get; set; }
}

/// <summary>
/// Маълумоти оморӣ дар бораи давомоти як хонанда
/// </summary>
public class StudentAttendanceAllStatisticsDto : AttendanceAllStatisticsDto
{
    /// <summary>
    /// Рамзи хонанда дар система
    /// </summary>
    public int StudentId { get; set; }

    /// <summary>
    /// Ному насаби хонанда
    /// </summary>
    public required string StudentName { get; set; }

    /// <summary>
    /// Рамзи гурӯҳ
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>
    /// Номи гурӯҳ
    /// </summary>
    public required string GroupName { get; set; }
}

/// <summary>
/// Маълумоти оморӣ дар бораи давомот барои як гурӯҳ
/// </summary>
public class GroupAttendanceAllStatisticsDto : AttendanceAllStatisticsDto
{
    /// <summary>
    /// Рамзи гурӯҳ
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>
    /// Номи гурӯҳ
    /// </summary>
    public required string GroupName { get; set; }

    /// <summary>
    /// Миқдори умумии хонандагон дар гурӯҳ
    /// </summary>
    public int TotalStudents { get; set; }

    /// <summary>
    /// Рӯйхати хонандагоне, ки давомоти хуб доранд
    /// </summary>
    public List<StudentAttendanceAllStatisticsDto> TopStudents { get; set; } = new();

    /// <summary>
    /// Рӯйхати хонандагоне, ки давомоти суст доранд
    /// </summary>
    public List<StudentAttendanceAllStatisticsDto> LowAttendanceStudents { get; set; } = new();
}

/// <summary>
/// Маълумоти оморӣ дар бораи давомот барои як марказ
/// </summary>
public class CenterAttendanceAllStatisticsDto : AttendanceAllStatisticsDto
{
    /// <summary>
    /// Рамзи марказ
    /// </summary>
    public int CenterId { get; set; }

    /// <summary>
    /// Номи марказ
    /// </summary>
    public required string CenterName { get; set; }

    /// <summary>
    /// Миқдори умумии гурӯҳҳо дар марказ
    /// </summary>
    public int TotalGroups { get; set; }

    /// <summary>
    /// Миқдори умумии хонандагон дар марказ
    /// </summary>
    public int TotalStudents { get; set; }

    /// <summary>
    /// Маълумоти оморӣ барои ҳар як гурӯҳи марказ
    /// </summary>
    public List<GroupAttendanceAllStatisticsDto> GroupStatistics { get; set; } = new();
}
