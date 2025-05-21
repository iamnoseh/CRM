namespace Domain.DTOs.Grade;

/// <summary>
/// Универсальный DTO для обновления оценки (как для урока, так и для экзамена)
/// </summary>
public class UpdateGradeDto:CreateGradeDto
{
    public int Id { get; set; }
}
