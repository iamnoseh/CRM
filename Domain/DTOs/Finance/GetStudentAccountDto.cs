namespace Domain.DTOs.Finance;

public class GetStudentAccountDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
}


