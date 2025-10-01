namespace Domain.DTOs.StudentGroup;

public class CreateStudentGroup
{
    public int StudentId { get; set; }
    public int GroupId { get; set; }
    public bool IsActive { get; set; } = true;
}