namespace Domain.DTOs.Grade;

public class GetExamGradeDto : CreateExamGradeDto
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}