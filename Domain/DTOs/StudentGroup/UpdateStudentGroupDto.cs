namespace Domain.DTOs.StudentGroup;

public class UpdateStudentGroupDto
{
    public required int Id { get; set; }
    public int? GroupId { get; set; }
    public int? StudentId { get; set; }
    public bool? IsActive { get; set; }
}