using Domain.Enums;

namespace Domain.DTOs.Finance;

public class CreateExpenseDto
{
    public int CenterId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset ExpenseDate { get; set; }
    public ExpenseCategory Category { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Description { get; set; }
    public int? MentorId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
}

