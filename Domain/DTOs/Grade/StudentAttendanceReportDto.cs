using System;
using System.Collections.Generic;

namespace Domain.DTOs.Grade;

/// <summary>
/// DTO для отчета об успеваемости и посещаемости студента
/// </summary>
public class StudentAttendanceReportDto
{
    /// <summary>
    /// Идентификатор студента
    /// </summary>
    public int StudentId { get; set; }
    
    /// <summary>
    /// Полное имя студента
    /// </summary>
    public string FullName { get; set; }
    
    /// <summary>
    /// Оценки и посещаемость по дням
    /// </summary>
    public List<DailyAttendanceDto> DailyAttendance { get; set; } = new();
    
    /// <summary>
    /// Бонусные баллы
    /// </summary>
    public int Bonus { get; set; }
    
    /// <summary>
    /// Результат экзамена
    /// </summary>
    public int? ExamPoints { get; set; }
    
    /// <summary>
    /// Общая сумма баллов (оценки + посещаемость + бонусы + экзамен)
    /// </summary>
    public int Total { get; set; }
}

/// <summary>
/// DTO для ежедневной посещаемости и оценки
/// </summary>
public class DailyAttendanceDto
{
    /// <summary>
    /// Дата урока
    /// </summary>
    public DateTimeOffset Date { get; set; }
    
    /// <summary>
    /// Присутствовал ли студент
    /// </summary>
    public bool WasPresent { get; set; }
    
    /// <summary>
    /// Оценка за день
    /// </summary>
    public int? Grade { get; set; }
}
