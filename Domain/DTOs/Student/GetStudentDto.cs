using Domain.Enums;

namespace Domain.DTOs.Student;

public class GetStudentDto
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
    public string? Document { get; set; }
    public string? Role { get; set; }
    public int UserId { get; set; }
    public int CenterId { get; set; }
}