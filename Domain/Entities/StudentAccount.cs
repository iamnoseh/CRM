namespace Domain.Entities;

public class StudentAccount : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; }

    public string AccountCode { get; set; } = string.Empty; // 6-digit code

    public decimal Balance { get; set; }

    public bool IsActive { get; set; } = true;
}


