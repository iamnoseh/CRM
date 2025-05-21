namespace Domain.DTOs.Exam;

/// <summary>
/// DTO для создания экзамена
/// </summary>
public class CreateExamDto
{
    public int Id { get; set; }
    public DateTimeOffset ExamDate { get; set; } = DateTimeOffset.Now;
    public int GroupId { get; set; }
    public int WeekIndex { get; set; }
}