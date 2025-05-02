namespace Domain.Entities;

public class MonthlySummary : BaseEntity
{
    public int CenterId { get; set; }
    public Center Center { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int StudentsWithPaymentIssues { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PendingPayments { get; set; }
    public decimal AverageAttendanceRate { get; set; }
    public decimal AverageGrade { get; set; }
    public string? Notes { get; set; }
    public DateTime GeneratedDate { get; set; } = DateTime.Now;
    public bool IsClosed { get; set; } = false;
}
