namespace Domain.DTOs.Exam;

/// <summary>
/// DTO для создания экзамена
/// </summary>
public class CreateExamDto
{
    /// <summary>
    /// Дата проведения экзамена
    /// </summary>
    public DateTimeOffset ExamDate { get; set; } = DateTimeOffset.Now;

    
    /// <summary>
    /// ID группы
    /// </summary>
    public int GroupId { get; set; }
    
    /// <summary>
    /// Индекс недели
    /// </summary>
    public int WeekIndex { get; set; }
    
    /// <summary>
    /// Максимально возможное количество баллов за экзамен
    /// </summary>
    public int MaxPoints { get; set; } = 100;
}