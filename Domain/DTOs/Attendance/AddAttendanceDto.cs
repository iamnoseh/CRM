using Domain.Enums;

namespace Domain.DTOs.Attendance;

public class AddAttendanceDto
{
    public AttendanceStatus Status { get; set; }
    public int LessonId { get; set; }
    public int StudentId { get; set; }
    public int GroupId { get; set; }
}