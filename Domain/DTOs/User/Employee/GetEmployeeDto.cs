using Domain.Enums;

namespace Domain.DTOs.User.Employee;

public class GetEmployeeDto 
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Role { get; set; }
    public decimal? Salary { get; set; }
    public DateTime? Birthday { get; set; }
    public int? Age { get; set; }
    public int? Experience { get; set; }
    public Gender Gender { get; set; }
    public ActiveStatus ActiveStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? ImagePath { get; set; }
    public string? DocumentPath { get; set; }
    public int? CenterId { get; set; }
}