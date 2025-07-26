using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Classroom;

public class UpdateClassroomDto
{
    [Required(ErrorMessage = "ID зарур аст")]
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Номи синфхона зарур аст")]
    [StringLength(100, ErrorMessage = "Номи синфхона набояд аз 100 ҳарф зиёд бошад")]
    public required string Name { get; set; }
    
    [StringLength(500, ErrorMessage = "Тавсиф набояд аз 500 ҳарф зиёд бошад")]
    public string? Description { get; set; }
    
    [Range(1, 200, ErrorMessage = "Гунҷоиш бояд аз 1 то 200 нафар бошад")]
    public int? Capacity { get; set; }
    
    public bool IsActive { get; set; } = true;
} 