using System;

namespace Domain.DTOs.StudentGroup;

public class LeftStudentDto
{
    public int StudentId { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? LeftReason { get; set; }
    public DateTime? LeftDate { get; set; }
}


