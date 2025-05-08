namespace Domain.DTOs.Exam;

/// <summary>
/// DTO для обновления экзамена
/// </summary>
public class UpdateExamDto
{
    /// <summary>
    /// Дата проведения экзамена
    /// </summary>
    public DateTimeOffset? ExamDate { get; set; }
    
    /// <summary>
    /// Описание или тема экзамена
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Индекс недели
    /// </summary>
    public int? WeekIndex { get; set; }
    
    /// <summary>
    /// Максимально возможное количество баллов за экзамен
    /// </summary>
    public int? MaxPoints { get; set; }
}