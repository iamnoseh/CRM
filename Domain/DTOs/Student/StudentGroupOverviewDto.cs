using Domain.Enums;

namespace Domain.DTOs.Student;

public class StudentGroupOverviewDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? CourseName { get; set; }
    public string? CourseImagePath { get; set; }
    public string? GroupImagePath { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public decimal AverageScore { get; set; }
    public decimal AttendanceRatePercent { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
}


