namespace Domain.DTOs.StudentGroup;

public class GetStudentGroupDto
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string? StudentFullName { get; set; } = string.Empty;
    public DateTimeOffset JoinedDate { get; set; }
    public bool IsActive { get; set; }
}