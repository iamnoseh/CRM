namespace Domain.DTOs.Finance;

public class GetAccountLogDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? PerformedByUserId { get; set; }
    public string? PerformedByName { get; set; }
}


