using System;
using System.Collections.Generic;

namespace Domain.DTOs.Statistics;

public class PaymentStatisticsDto
{
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal UnpaidAmount { get; set; }
    public int TotalPayments { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public double PaymentPercentage => TotalAmount > 0 
        ? Math.Round((double)(PaidAmount / TotalAmount * 100), 2) 
        : 0;
}

public class StudentPaymentStatisticsDto : PaymentStatisticsDto
{
    public int StudentId { get; set; }
    public required string StudentName { get; set; }
    public int GroupId { get; set; }
    public required string GroupName { get; set; }
    public List<PaymentDetailDto> RecentPayments { get; set; } = new();
}

public class GroupPaymentStatisticsDto : PaymentStatisticsDto
{
    public int GroupId { get; set; }
    public required string GroupName { get; set; }
    public int TotalStudents { get; set; }
    public List<StudentPaymentStatisticsDto> UnpaidStudents { get; set; } = new();
    public List<PaymentDetailDto> RecentPayments { get; set; } = new();
}

public class CenterPaymentStatisticsDto : PaymentStatisticsDto
{
    public int CenterId { get; set; }
    public required string CenterName { get; set; }
    public int TotalGroups { get; set; }
    public int TotalStudents { get; set; }
    public List<GroupPaymentStatisticsDto> GroupStatistics { get; set; } = new();
}

public class PaymentDetailDto
{
    public int PaymentId { get; set; }
    public int StudentId { get; set; }
    public required string StudentName { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset PaymentDate { get; set; }
    public required string PaymentMethod { get; set; }
}
