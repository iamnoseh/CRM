namespace Domain.DTOs.Grade;

public class GetLessonGradeDto : CreateLessonGradeDto
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}