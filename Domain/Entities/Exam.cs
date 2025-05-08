namespace Domain.Entities;

/// <summary>
/// Сущность экзамена для группы
/// </summary>
public class Exam : BaseEntity
{
    /// <summary>
    /// Индекс недели
    /// </summary>
    public int WeekIndex { get; set; }
    
    /// <summary>
    /// Дата проведения экзамена
    /// </summary>
    public DateTimeOffset ExamDate { get; set; }
    
    
    /// <summary>
    /// ID группы
    /// </summary>
    public int GroupId { get; set; }
    
    /// <summary>
    /// Навигационное свойство для группы
    /// </summary>
    public Group Group { get; set; }
    
    /// <summary>
    /// Максимально возможное количество баллов за экзамен
    /// </summary>
    public int MaxPoints { get; set; } = 100;
    
    /// <summary>
    /// Коллекция оценок студентов за этот экзамен
    /// </summary>
    public ICollection<ExamGrade> ExamGrades { get; set; } = new List<ExamGrade>();
}