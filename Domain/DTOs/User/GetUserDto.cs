using Domain.Entities;
using Domain.Enums;

namespace Domain.DTOs.User;

public class GetUserDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public Gender? Gender { get; set; }
    public ActiveStatus? ActiveStatus { get; set; }
    public string? Role { get; set; }
}