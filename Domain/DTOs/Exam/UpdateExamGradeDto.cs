namespace Domain.DTOs.Exam;


public class UpdateExamGradeDto
{

    public int? Points { get; set; }
    public bool? HasPassed { get; set; }
    public string Comment { get; set; }
    public int? BonusPoints { get; set; }
}
