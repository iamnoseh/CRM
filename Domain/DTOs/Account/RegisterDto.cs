using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
namespace Domain.DTOs.Account;

public class RegisterDto
{
    [Required]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "FullName must be between 4 and 50 characters")]
    public string FullName { get; set; }
    [Required]
    [StringLength(50,MinimumLength = 4, ErrorMessage = "Username must be between 4 and 50 characters")]
    public string UserName { get; set; } = string.Empty;
    [Required]
    public DateTimeOffset Birthday { get; set; }
    public int Age { get; set; }
    public string PhoneNumber { get; set; } = string.Empty; 
    public string Address { get; set; } = string.Empty;
    [Required]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
    
    public IFormFile? ProfileImage { get; set; }
}