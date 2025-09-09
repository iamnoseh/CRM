using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.User;

public class UpdateProfilePictureDto
{
    [Required]
    public IFormFile ProfilePicture { get; set; }
}
