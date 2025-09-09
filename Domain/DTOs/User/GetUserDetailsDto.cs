using Domain.Entities;
using Domain.Enums;

namespace Domain.DTOs.User;

public class GetUserDetailsDto
{
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public Gender? Gender { get; set; }
    public ActiveStatus? ActiveStatus { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public string? Role { get; set; }
    public string? Image { get; set; }
    public string? DocumentPath { get; set; }
    public int? CenterId { get; set; }
    public string? CenterName { get; set; }
    public decimal? Salary { get; set; }
    public int Experience { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool TelegramNotificationsEnabled { get; set; }
    public string? TelegramChatId { get; set; }
}
