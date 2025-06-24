using Domain.Entities;
using Domain.Enums;

namespace Domain.DTOs.User;

public class GetUserDto
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public Gender? Gender { get; set; }
    public ActiveStatus? ActiveStatus { get; set; }
    public string? Role { get; set; }
    public string? Image { get; set; }
    public int? CenterId { get; set; }
}