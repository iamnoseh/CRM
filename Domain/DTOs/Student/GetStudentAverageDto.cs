namespace Domain.DTOs.Student;

public class GetStudentAverageDto
{
    public int StudentId { get; set; }
    public int GroupId { get; set; }
    public double Value { get; set; }
}