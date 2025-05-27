using System;
using Domain.Enums;

namespace Domain.Entities;


public class StudentAverage : BaseEntity
{
    public int StudentId { get; set; }
    public AverageType Type { get; set; }
    public double Value { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }

    public Student Student { get; set; }
}
