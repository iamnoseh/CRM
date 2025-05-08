namespace Domain.DTOs.Exam;

/// <summary>
/// DTO для получения оценки студента за экзамен
/// </summary>
public class GetExamGradeDto
{
    /// <summary>
    /// ID оценки
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// ID экзамена
    /// </summary>
    public int ExamId { get; set; }
    
    /// <summary>
    /// ID студента
    /// </summary>
    public int StudentId { get; set; }
    
    /// <summary>
    /// Имя студента
    /// </summary>
    public string StudentName { get; set; }
    
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
    public int? BonusPoints { get; set; }
}
