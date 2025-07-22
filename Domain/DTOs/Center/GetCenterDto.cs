namespace Domain.DTOs.Center;

public class GetCenterDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public decimal MonthlyIncome { get; set; }
    public decimal YearlyIncome { get; set; }
    public int StudentCapacity { get; set; }
    public bool IsActive { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerFullName { get; set; }
    
    // Дополнительные свойства, которые могут быть полезны при получении данных
    public int TotalStudents { get; set; }
    public int TotalMentors { get; set; }
    public int TotalCourses { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
