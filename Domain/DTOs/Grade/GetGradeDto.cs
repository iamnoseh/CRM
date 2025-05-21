namespace Domain.DTOs.Grade;

/// <summary>
/// Универсальный DTO для получения оценки (как для урока, так и для экзамена)
/// </summary>
public class GetGradeDto
{
    /// <summary>
    /// ID оценки
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// ID группы
    /// </summary>
    public int GroupId { get; set; }
    
    /// <summary>
    /// ID студента
    /// </summary>
    public int StudentId { get; set; }
    
    /// <summary>
    /// Имя студента
    /// </summary>
    public string? StudentName { get; set; }
    
    /// <summary>
    /// ID урока (если это оценка за урок)
    /// </summary>
    public int? LessonId { get; set; }
    
    /// <summary>
    /// ID экзамена (если это оценка за экзамен)
    /// </summary>
    public int? ExamId { get; set; }
    
    /// <summary>
    /// Оценка
    /// </summary>
    public int? Value { get; set; }
    
    /// <summary>
    /// Бонусные баллы
    /// </summary>
    public int? BonusPoints { get; set; }
    
    /// <summary>
    /// Комментарий к оценке
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// Индекс недели
    /// </summary>
    public int? WeekIndex { get; set; }
    
    /// <summary>
    /// Индекс дня
    /// </summary>
    public int? DayIndex { get; set; }
    
    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Тип оценки (урок или экзамен)
    /// </summary>
    public string GradeType => LessonId.HasValue ? "Lesson" : "Exam";
}
