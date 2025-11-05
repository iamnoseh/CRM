namespace Domain.Entities;

public class AccountLog : BaseEntity
{
    public int AccountId { get; set; }
    public StudentAccount Account { get; set; }

    public decimal Amount { get; set; } // + topup, - charge

    public string Type { get; set; } = string.Empty; // TopUp | MonthlyCharge | Refund | Adjustment

    public string? Note { get; set; }

    public int? PerformedByUserId { get; set; }
    public string? PerformedByName { get; set; }
}


