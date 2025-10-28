using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.User;

public class ChangeEmailDto
{
    [Required]
    [EmailAddress]
    public string NewEmail { get; set; }
}
