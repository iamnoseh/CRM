using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Domain.Entities;

public class Course : BaseEntity
{
    [Required]
    public required string CourseName { get; set; }
    [Required]
    public required string Description { get; set; }
    [Required]
    public string? ImagePath { get; set; }
    [NotMapped]
    public IFormFile? Image { get; set; }
    public int DurationInMonth { get; set; }
    public decimal Price { get; set; }
    public ActiveStatus Status { get; set; }
    public List<Group> Groups { get; set; } = new(); 
    public int CenterId { get; set; }
    public Center Center { get; set; }
}