using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;
public class User : IdentityUser<int>
{
    [Required]
    public string FullName { get; set; }
    public DateTime Birthday { get; set; }
    public Gender Gender { get; set; }
    public ActiveStatus ActiveStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? ProfileImagePath { get; set; }
    public int Age { get; set; }
    public string Address { get; set; }
    public string? Code { get; set; }
    public DateTime CodeDate { get; set; }
    public bool IsDeleted { get; set; }
    public string? TelegramChatId { get; set; }
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool TelegramNotificationsEnabled { get; set; } = true;
    public int? CenterId { get; set; }
    public Center? Center { get; set; }
    public Student? StudentProfile { get; set; }
    public Mentor? MentorProfile { get; set; }
}