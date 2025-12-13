using Domain.Enums;

namespace Domain.DTOs.Payroll;

public class PaymentHistoryDto
{
    public int Id { get; set; }
    
    public int? MentorId { get; set; }
    public string? MentorName { get; set; }
    
    public int? EmployeeUserId { get; set; }
    public string? EmployeeName { get; set; }
    
    public int Month { get; set; }
    public int Year { get; set; }
    public string Period => $"{Month:00}.{Year}";
    
    public decimal NetAmount { get; set; }
    
    public DateTime? PaidDate { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? PaymentMethodDisplay { get; set; }
    
    public string? Notes { get; set; }
}
