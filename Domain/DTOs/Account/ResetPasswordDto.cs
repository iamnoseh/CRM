using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Account;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "Token ҳатмист")]
    public string ResetToken { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Рамзи нав ҳатмист")]
    [MinLength(6, ErrorMessage = "Рамз набояд аз 6 рамз кам бошад")]
    public string NewPassword { get; set; } = string.Empty;
}