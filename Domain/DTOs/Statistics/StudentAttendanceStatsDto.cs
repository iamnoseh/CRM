namespace Domain.DTOs.Statistics;

public class StudentAttendanceStatsDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    
    // Шумораи дарсҳое, ки донишҷӯ ҳозир будааст
    public int PresentCount { get; set; }
    
    // Шумораи дарсҳое, ки донишҷӯ ғоиб будааст
    public int AbsentCount { get; set; }
    
    // Шумораи дарсҳое, ки донишҷӯ дер омадааст
    public int LateCount { get; set; }
    
    // Ҳамагӣ шумораи дарсҳо
    public int TotalLessonsCount { get; set; }
    
    // Фоизи иштирок (ҳозирӣ)
    public double AttendancePercentage { get; set; }
    
    // Фоизи пурраи иштирок (ҳозирӣ бе деромад)
    public double FullAttendancePercentage { get; set; }
    
    // Ҳафтаи охирини ғоибӣ
    public int? LastAbsentWeek { get; set; }
    
    // Шумораи рӯзҳои пай дар пайи ҳозирӣ
    public int ConsecutivePresentDays { get; set; }
}
