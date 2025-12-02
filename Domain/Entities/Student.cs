using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Domain.Entities;
public class Student : BaseEntity
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
    public DateTime Birthday { get; set; }
    public int Age { get; set; }
    public string? Document { get; set; }
    [NotMapped]
    public IFormFile? DocumentFile { get; set; }
    public Gender Gender { get; set; }
    public ActiveStatus ActiveStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal Discount { get; set; } = 0;
    public string? ProfileImage { get; set; }
    public decimal TotalPaid { get; set; } = 0;
    public DateTime? LastPaymentDate { get; set; }
    public DateTime? NextPaymentDueDate { get; set; }
    public int CenterId { get; set; }
    public Center? Center { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public List<StudentGroup> StudentGroups { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public List<JournalEntry> JournalEntries { get; set; } = new();
}