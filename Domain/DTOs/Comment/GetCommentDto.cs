namespace Domain.DTOs.Comment;

public class GetCommentDto : CreateCommentDto
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? StudentName { get; set; }
    public string? GroupName { get; set; }
    public string? AuthorName { get; set; }
    public DateTime CommentDate { get; set; }
}