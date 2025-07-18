using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.User.Employee;

public class CreateEmployeeDto
{
    [Required] public required string FullName { get; set; }
    [Required] [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public required string Email { get; set; }
    [Required] public string Address { get; set; } = string.Empty;
    [Required] [StringLength(13, MinimumLength = 9, ErrorMessage = "Phone number must be between 9 and 13 characters")]
    public required string PhoneNumber { get; set; }
    [Required] public required Role Role { get; set; }
    public decimal Salary { get; set; }
    public DateTime Birthday { get; set; }
    public int Experience { get; set; }
    public Gender Gender { get; set; }
    public ActiveStatus ActiveStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public IFormFile? Image { get; set; }
    public IFormFile? Document { get; set; }
    public int CenterId { get; set; }
}