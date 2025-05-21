namespace Domain.DTOs.Exam;

/// <summary>
/// DTO для обновления экзамена
/// </summary>
public class UpdateExamDto
{
    /// <summary>
    /// Идентификатор экзамена
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Дата проведения экзамена
    /// </summary>
    public DateTimeOffset? ExamDate { get; set; }
    
    /// <summary>
    /// Индекс недели
    /// </summary>
    public int? WeekIndex { get; set; }
}