using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;

public class JournalEntry : BaseEntity
{
    [Required]
    public int JournalId { get; set; }
    public Journal? Journal { get; set; }
    [Required]
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    [Required]
    [Range(1, 7)]
    public int DayOfWeek { get; set; }
    [Required]
    [Range(1, 6)]
    public int LessonNumber { get; set; }
    public LessonType LessonType { get; set; } = LessonType.Regular;
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    [Range(0, 100)]
    public decimal? Grade { get; set; }
    [Range(0, 30)]
    public decimal? BonusPoints { get; set; }
    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Absent;
    [StringLength(500)]
    public string? Comment { get; set; }
    public CommentCategory? CommentCategory { get; set; }
    public int? CommentAuthorId { get; set; }
    [StringLength(200)]
    public string? CommentAuthorName { get; set; }
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
}