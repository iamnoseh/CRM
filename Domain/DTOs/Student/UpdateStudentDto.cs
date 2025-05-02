using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.Student;

public class UpdateStudentDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public required string Email { get; set; }
    public string Address { get; set; } = string.Empty;

    public required string PhoneNumber { get; set; }
    public DateTime Birthday { get; set; }
    public Gender Gender { get; set; }
    public ActiveStatus ActiveStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public IFormFile? ProfilePhoto { get; set; }

}