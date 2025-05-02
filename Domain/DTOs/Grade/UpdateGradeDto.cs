namespace Domain.DTOs.Grade;

public class UpdateGradeDto
{
    public int Id { get; set; }
    public int? Value {get; set;}
    public string? Comment {get; set;}
    public int? BonusPoints {get; set;}
}