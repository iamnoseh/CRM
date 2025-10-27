namespace Domain.DTOs.StudentGroup;

public class UpdateStudentGroupDto
{
    public int? GroupId { get; set; }
    public int? StudentId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsLeft { get; set; }
    public string? LeftReason { get; set; }
    public DateTime? LeftDate { get; set; }
}