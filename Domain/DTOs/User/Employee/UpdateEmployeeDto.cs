using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.User.Employee;

public class UpdateEmployeeDto 
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Role? Role { get; set; }
    public decimal? Salary { get; set; }
    public DateTime? Birthday { get; set; }
    public int? Age { get; set; }
    public int? Experience { get; set; }
    public Gender? Gender { get; set; }
    public ActiveStatus? ActiveStatus { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public IFormFile? Image { get; set; }
    public IFormFile? Document { get; set; }
    public int? CenterId { get; set; }
}