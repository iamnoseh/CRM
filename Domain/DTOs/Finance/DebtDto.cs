namespace Domain.DTOs.Finance;

public class DebtDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}


