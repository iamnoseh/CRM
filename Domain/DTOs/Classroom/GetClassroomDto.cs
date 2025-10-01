using Domain.DTOs.Center;

namespace Domain.DTOs.Classroom;

public class GetClassroomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public bool IsActive { get; set; }
    public int CenterId { get; set; }
    public GetCenterSimpleDto Center { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
} 