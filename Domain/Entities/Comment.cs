using Domain.Enums;

namespace Domain.Entities;

public class Comment : BaseEntity
{
    public string? Text { get; set; }
    public int StudentId { get; set; }
    public int GroupId { get; set; }
    public int LessonId { get; set; }
    public int? AuthorId { get; set; }  
    public DateTime CommentDate { get; set; } = DateTime.Now;
    
    public Student Student { get; set; }
    public Group Group { get; set; }
    public Lesson Lesson { get; set; }
}