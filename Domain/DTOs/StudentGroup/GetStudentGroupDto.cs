namespace Domain.DTOs.StudentGroup;

public class GetStudentGroupDto
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; } = string.Empty;
    public StudentDTO student { get; set; }
    public bool IsActive { get; set; }
}

public class StudentDTO
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public int Age { get; set; }
    public string PhoneNumber { get; set; }
    public DateTimeOffset JoinedDate { get; set; }
}