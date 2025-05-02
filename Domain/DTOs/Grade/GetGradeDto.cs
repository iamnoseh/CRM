namespace Domain.DTOs.Grade;

public class GetGradeDto : CreateGradeDto
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}