using Domain.Enums;

namespace Domain.Entities;

public class Expense : BaseEntity
{
    public int CenterId { get; set; }
    public Center Center { get; set; }

    public decimal Amount { get; set; }
    public DateTimeOffset ExpenseDate { get; set; }

    public ExpenseCategory Category { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public string? Description { get; set; }

    public int? MentorId { get; set; }
    public Mentor? Mentor { get; set; }

    public int Month { get; set; }
    public int Year { get; set; }
}

