namespace Domain.Entities;

/// <summary>
/// Сущность оценки за экзамен для конкретного студента
/// </summary>
public class ExamGrade : BaseEntity
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
    /// Количество баллов, полученных студентом
    /// </summary>
    public int Points { get; set; }
    
    /// <summary>
    /// Флаг, прошел ли студент экзамен успешно
    /// </summary>
    public bool? HasPassed { get; set; }
    
    /// <summary>
    /// Комментарий преподавателя к оценке
    /// </summary>
    public string Comment { get; set; }
    public int BonusPoint { get; set; }
    
    /// <summary>
    /// Навигационное свойство для экзамена
    /// </summary>
    public Exam Exam { get; set; }
    
    /// <summary>
    /// Навигационное свойство для студента
    /// </summary>
    public User Student { get; set; }
}
