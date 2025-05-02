using Domain.Enums;

namespace Domain.DTOs.Comment;

public class CreateCommentDto
{
    public string? Text { get; set; }
    public int StudentId { get; set; }
    public int GroupId { get; set; }
    public int LessonId { get; set; }
    public int? AuthorId { get; set; }
    public bool IsPrivate { get; set; } = false;
}