using Domain.Enums;

namespace Domain.DTOs.Journal;

public class StudentCommentDto
{
    public int Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int WeekNumber { get; set; }
    public string Comment { get; set; } = string.Empty;
    public CommentCategory CommentCategory { get; set; }
    public string CommentCategoryName { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public int LessonNumber { get; set; }
}

