namespace Domain.Entities;

public class StudentStatistics : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; }
    public double AverageGrade { get; set; }
    public double TotalAverage { get; set; }
    public int AttendanceCount { get; set; }
    public int AbsenceCount { get; set; }
    public double AttendancePercentage { get; set; }
    public int WeekIndex { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}
