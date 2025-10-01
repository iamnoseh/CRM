using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.DTOs.Journal;

public class UpdateJournalEntryDto
{
    [Range(0, 100)]
    public decimal? Grade { get; set; }
    [Range(0, 30)]
    public decimal? BonusPoints { get; set; }
    public AttendanceStatus? AttendanceStatus { get; set; }
    public string? Comment { get; set; }
    public CommentCategory? CommentCategory { get; set; }
}


