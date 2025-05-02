using Domain.Enums;

namespace Domain.DTOs.Mentor;

public class GetMentorDto
{
    public int Id { get; set; }
    public string? FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;   
    public string? Address { get; set; } = string.Empty;
    public DateTime Birthday { get; set; }
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public ActiveStatus ActiveStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? ImagePath { get; set; }
    public decimal? Salary { get; set; }
    public int CenterId { get; set; }
}