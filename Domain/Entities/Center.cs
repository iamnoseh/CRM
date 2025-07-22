using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Domain.Entities;

public class Center : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    [NotMapped]
    public IFormFile ImageFile { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal YearlyIncome { get; set; }
    public int StudentCapacity { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Email { get; set; }
    public string? ContactPhone { get; set; }
    
    public int? ManagerId { get; set; }
    public User? Manager { get; set; }

    public List<Course> Courses { get; set; } = [];
    public List<Student> Students { get; set; } = [];
    public List<Mentor> Mentors { get; set; } = [];
    public List<User> Users { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<MonthlySummary> MonthlySummaries { get; set; } = [];
}