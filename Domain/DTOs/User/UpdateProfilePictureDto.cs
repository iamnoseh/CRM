using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.User;

public class UpdateProfilePictureDto
{
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public IFormFile ProfilePicture { get; set; }
}
