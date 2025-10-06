using Domain.DTOs.Discounts;
using Domain.Enums;

namespace Domain.DTOs.StudentGroup;

public class GetStudentGroupDto
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; } = string.Empty;
    public StudentDTO student { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime? LeaveDate { get; set; }
}

public class StudentDTO
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public int Age { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime JoinedDate { get; set; } 
    public PaymentStatus PaymentStatus { get; set; }
    public GetStudentGroupDiscountDto Discount { get; set; } = new();
}
