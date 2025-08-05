namespace Domain.Entities;

public class JournalEntry :  BaseEntity
{
    public StudentGroup StudentGroup { get; set; }
    public Group Group { get; set; }
}