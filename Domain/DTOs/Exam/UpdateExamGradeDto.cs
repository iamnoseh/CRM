namespace Domain.DTOs.Exam;

/// <summary>
/// DTO для обновления оценки студента за экзамен
/// </summary>
public class UpdateExamGradeDto
{
    /// <summary>
    /// Количество набранных баллов
    /// </summary>
    public int? Points { get; set; }
    
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
