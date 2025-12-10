using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.Mentor;

public class CreateMentorDto
{
    public string FullName { get; set; } = string.Empty;
    public required string Email { get; set; }
    public string Address { get; set; } = string.Empty;
    public required string PhoneNumber { get; set; }
    public int Experience { get; set; }
    public DateTime Birthday { get; set; }
    public Gender Gender { get; set; }
    public IFormFile? ProfileImage { get; set; }
    public IFormFile? DocumentFile { get; set; }
}