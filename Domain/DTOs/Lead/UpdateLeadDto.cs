using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.DTOs.Lead;

public class UpdateLeadDto
{
    [Required(ErrorMessage = "ID зарур аст")]
    public int Id { get; set; }
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Номи пурра бояд аз 2 то 100 ҳарф бошад")]
    public string? FullName { get; set; }
    [StringLength(13, MinimumLength = 9, ErrorMessage = "Рақами телефон бояд аз 9 то 13 рақам бошад")]
    public string? PhoneNumber { get; set; }
    public DateTime BirthDate { get; set; }
    public Gender Gender { get; set; }
    public OccupationStatus OccupationStatus { get; set; }
    public DateTime? RegisterForMonth { get; set; }
    [StringLength(100, ErrorMessage = "Номи курс наметавонад аз 100 ҳарф зиёд бошад")]
    public string? Course { get; set; }
    public TimeSpan LessonTime { get; set; }
    [StringLength(500, ErrorMessage = "Қайдҳо наметавонанд аз 500 ҳарф зиёд бошанд")]
    public string? Notes { get; set; }
}