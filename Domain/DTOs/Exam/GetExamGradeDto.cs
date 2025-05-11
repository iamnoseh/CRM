namespace Domain.DTOs.Exam;

public class GetExamGradeDto
{
    public int Id { get; set; }
    
    public int ExamId { get; set; }
    
    public int StudentId { get; set; }
    
    public string StudentName { get; set; }

    public int Points { get; set; }
  
    public bool? HasPassed { get; set; }
    

    public string Comment { get; set; }
    
    public int? BonusPoints { get; set; }
}
