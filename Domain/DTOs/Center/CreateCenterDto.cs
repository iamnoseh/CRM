using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.Center;

public class CreateCenterDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Address { get; set; } = null!;
    
    [Required]
    [Phone]
    public string ContactPhone { get; set; } = null!;
    
    [EmailAddress]
    public string? ContactEmail { get; set; }
    
    public string? ManagerName { get; set; }
    
    public IFormFile? ImageFile { get; set; }
    
    public int StudentCapacity { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
}
