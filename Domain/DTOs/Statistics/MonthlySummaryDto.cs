namespace Domain.DTOs.Statistics;

public class MonthlySummaryDto
{
    public int Id { get; set; }
    public int CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    
    // Шумораи умумии донишҷӯён
    public int TotalStudents { get; set; }
    
    // Шумораи донишҷӯёни фаъол
    public int ActiveStudents { get; set; }
    
    // Шумораи донишҷӯёне, ки бо пардохт мушкилӣ доранд
    public int StudentsWithPaymentIssues { get; set; }
    
    // Даромади умумӣ
    public decimal TotalRevenue { get; set; }
    
    // Пардохтҳои интизорӣ
    public decimal PendingPayments { get; set; }
    
    // Миёнаи ҳузур
    public decimal AverageAttendanceRate { get; set; }
    
    // Миёнаи баҳо
    public decimal AverageGrade { get; set; }
    
    // Эзоҳ
    public string? Notes { get; set; }
    
    // Санаи тайёр кардани ҳисобот
    public DateTime GeneratedDate { get; set; }
    
    // Оё ҳисобот ниҳоӣ аст (баста шудааст)
    public bool IsClosed { get; set; }
    
    // Маълумот оид ба гурӯҳҳо
    public List<GroupMonthlySummaryDto> GroupSummaries { get; set; } = new();
}
