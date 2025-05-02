using Domain.Enums;

namespace Domain.Entities;
public class Attendance : BaseEntity
{
    public AttendanceStatus Status { get; set; }
    public int StudentId { get; set; }
    public int LessonId { get; set; }
    public int GroupId { get; set; }
    
    public Group Group { get; set; }
    public Lesson Lesson { get; set; }
    public Student Student { get; set; }
}