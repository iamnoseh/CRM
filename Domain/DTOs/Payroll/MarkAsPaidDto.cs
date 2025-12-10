using Domain.Enums;

namespace Domain.DTOs.Payroll;

public class MarkAsPaidDto
{
    public PaymentMethod PaymentMethod { get; set; }
    public string? Notes { get; set; }
}
