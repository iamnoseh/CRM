namespace Domain.DTOs.Exam;

/// <summary>
/// DTO для создания оценки студента за экзамен
/// </summary>
public class CreateExamGradeDto
{
    /// <summary>
    /// ID экзамена
    /// </summary>
    public int ExamId { get; set; }
    
    /// <summary>
    /// ID студента
    /// </summary>
    public int StudentId { get; set; }
    
    /// <summary>
    /// Количество набранных баллов
    /// </summary>
    public int Points { get; set; }
    
    /// <summary>
    /// Успешно ли сдан экзамен
    /// </summary>
    public bool? HasPassed { get; set; }
    
    /// <summary>
    /// Комментарий к оценке
    /// </summary>
    public string Comment { get; set; }
    
    /// <summary>
    /// Количество бонусных баллов
    /// </summary>
    public int BonusPoints { get; set; }
}
