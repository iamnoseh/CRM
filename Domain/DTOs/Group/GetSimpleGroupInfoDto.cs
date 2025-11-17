namespace Domain.DTOs.Group;

public class GetSimpleGroupInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
}