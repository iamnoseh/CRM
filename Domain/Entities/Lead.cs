using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;


public class Lead : BaseEntity
{

    [Required(ErrorMessage = "Номи пурра зарур аст")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Номи пурра бояд аз 2 то 100 ҳарф бошад")]
    public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Рақами телефон зарур аст")]
    [StringLength(13, MinimumLength = 9, ErrorMessage = "Рақами телефон бояд аз 9 то 13 рақам бошад")]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required(ErrorMessage = "Санаи таваллуд зарур аст")]
    public DateTime BirthDate { get; set; }
    
    [Required(ErrorMessage = "Ҷинс зарур аст")]
    public Gender Gender { get; set; }
    
    [Required(ErrorMessage = "Ҳолати касб зарур аст")]
    public OccupationStatus OccupationStatus { get; set; }
    public DateTime? RegisterForMonth { get; set; }
    
    [StringLength(100, ErrorMessage = "Номи курс наметавонад аз 100 ҳарф зиёд бошад")]
    public string? Course { get; set; } = string.Empty;
    public TimeSpan LessonTime { get; set; }
    
    [StringLength(500, ErrorMessage = "Қайдҳо наметавонанд аз 500 ҳарф зиёд бошанд")]
    public string Notes { get; set; } = string.Empty;
    
    [StringLength(100, ErrorMessage = "UTM source наметавонад аз 100 ҳарф зиёд бошад")]
    public string UtmSource { get; set; } = string.Empty;
    [Required(ErrorMessage = "ID-и марказ зарур аст")]
    public int CenterId { get; set; }
    public Center? Center { get; set; }
}

