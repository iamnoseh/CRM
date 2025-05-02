using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;
public class Mentor : BaseEntity
{
    [Required]
    public required string FullName { get; set; }


    [Required]
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public required string Email { get; set; }
    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(13, MinimumLength = 9, ErrorMessage = "Phone number must be between 9 and 13 characters")]
    public required string PhoneNumber { get; set; }
    public decimal Salary { get; set; }
    public DateTime Birthday { get; set; }
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public ActiveStatus ActiveStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? ProfileImage { get; set; }
    
    public int UserId { get; set; }
    public int CenterId { get; set; }
    public Center Center { get; set; }
    public User User { get; set; }
    public List<Group> Groups { get; set; } = new();
}